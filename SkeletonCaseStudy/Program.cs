﻿using System.Collections.Generic;
using System.Linq;
using GrammarMetaModel;
using AlgorithmMetaModel;
using Rhino.Geometry;
using Rhino.FileIO;
using System;

namespace SkeletonCaseStudy
{
    class Program
    {
        /// <summary>
        /// Script illustrating (only) the fundamental building blocks of the algorithmic design framework (to be further expanded)
        /// To scale up the complexity of the method bit by bit, i would recommend to maybe proceed as follows:
        ///     Assemble entire field(single-storey) with beam and deck with varying x-y-z-extensions
        ///     Vary also number and height of storeys
        ///     Introduce possibility to vary number of fields ("intermediary column" module necessary...)
        ///     Generate two different fields with different number of storeys
        ///     ...
        /// </summary>
        static void Main(string[] args)
        {
            // For elegancy, the rule definition could be outsourced to a separate grasshopper definition file (maybe with some custom grasshopper components and previews etc. (compare WASP).
            // For now, i did it like this
            #region RuleDefinition

            // Part Definitions
            Part foundation = new Part("foundation", "foundation.ghx");
            Part column = new Part("column", "column.ghx");

            Part beam = new Part("beam", "beam.ghx");
            Part deck = new Part("deck", "deck.ghx");

            // Rule Definitions
            RuleDefinition columnOnFoundation = new RuleDefinition(
                "ColumnOnFoundation",
                foundation,
                foundation.Connections.First(),
                column.Connections.Where(c => c.Name == "Foundation").First(),
                column);

            RuleDefinition beamOnColumnConsoleYZ = new RuleDefinition(
                "BeamOnColumnConsoleYZ",
                column,
                column.Connections.Where(c => c.Name == "BeamYZ").First(),
                beam.Connections.Where(c => c.Name == "CloserColumn").First(),
                beam);

            RuleDefinition beamOnColumnConsoleXZ = new RuleDefinition(
                "BeamOnColumnConsoleXZ",
                column,
                column.Connections.Where(c => c.Name == "BeamXZ").First(),
                beam.Connections.Where(c => c.Name == "RearerColumn").First(),
                beam);

            RuleDefinition deckOnColumn = new RuleDefinition(
                "DeckOnColumn",
                column,
                column.Connections.Where(c => c.Name == "Deck").First(),
                deck.Connections.Where(c => c.Name == "ToAnyBottomColumn").First(),
                deck);

            RuleDefinition columnOnDeck = new RuleDefinition(
                "ColumnOnDeck",
                deck,
                deck.Connections.Where(c => c.Name == "ToTopColumns").First(),
                column.Connections.Where(c => c.Name == "Foundation").First(),
                column);

            RuleCatalogue rules = new RuleCatalogue(
                "OneField", new List<RuleDefinition> { columnOnFoundation, beamOnColumnConsoleYZ, beamOnColumnConsoleXZ, deckOnColumn, columnOnDeck },
                 new List<Part> { foundation, column, beam, deck });
            #endregion RuleDefinition

            // The setup of the entire process model should happen prior to any rule execution i think. 
            // For now, the process model is only a linear one-after-the-other chain (of standardised blocks of type start/planning/assembly state).
            // Further types of states and non-linear things (like parallel/ if-else tracks) can be imagined)
            #region ProcessModelSetup
            // start symbol setup 
            Rectangle3d dummyPlot = new Rectangle3d(Plane.WorldXY, new Point3d(0.0, 0.0, -3.0), new Point3d(10.0, 14.0, -3.0));
            CustomisationSettings projectParameters = new CustomisationSettings();
            projectParameters.Parameters["NumberOfFields"] =  1.0;
            projectParameters.Parameters["NumberOfStoreys"] = 1.0;
            projectParameters.Parameters["StoreyHeight"] = 3.5;

            //// process model
            ProcessModel skeletonProcessModel = new ProcessModel(rules);
            skeletonProcessModel.AddStartState(new StartStateSkeleton(dummyPlot, foundation));
            PlanningStateBasementColumns planningColumnAssembly = new PlanningStateBasementColumns(rules, projectParameters);
            AssemblingState assemblingColumns = new AssemblingState();
            skeletonProcessModel.AddRelatedPlanningAndAssemblingState(planningColumnAssembly, assemblingColumns);
            #endregion ProcessModelSetup


            // The execution of a valid process model should be straightforward, with the possibility to export intermediary design model states.
            #region ProcessModelExecution
            //execution of process model
            bool endStateNotReached = true;
            int i = 0;
            while (endStateNotReached)
            {
                endStateNotReached = skeletonProcessModel.MakeStep();
                skeletonProcessModel.DesignGraph.SerializeToThreeDm(i); //For planning states, this produces a 3dm file without any geometric change happening --> to be fixed
                i++;
            }
            #endregion ProcessModelExecution


        }
    }
}

