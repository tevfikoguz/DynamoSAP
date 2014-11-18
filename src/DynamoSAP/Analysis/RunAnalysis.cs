﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//DYNAMO
using Autodesk.DesignScript.Geometry;
using Autodesk.DesignScript.Runtime;

//SAP 
using SAP2000v16;
using SAPConnection;
using DynamoSAP.Assembly;
using DynamoSAP.Structure;

namespace DynamoSAP.Analysis
{
    public class Analysis : IResults
    {
        public List<FrameResults> FrameResults { get; set; }
        private static cSapModel mySapModel;

        public static StructuralModel Run(StructuralModel Model, string Filepath, bool RunIt)
        {
            if (RunIt)
            {
                // open sap     
                SAPConnection.Initialize.OpenSAPModel(Filepath, ref mySapModel);
                // run analysis
                SAPConnection.AnalysisMapper.RunAnalysis(ref mySapModel, Filepath);
            }
            return Model;
        }

        public static Analysis GetResults(StructuralModel Model, string LoadCase, bool Run)
        {
            List<FrameResults> frameResults = null;
            Analysis StructureResults = new Analysis();
            if (Run)
            {
                // loop over frames get results and populate to dictionary
                frameResults = SAPConnection.AnalysisMapper.GetFrameForces(ref mySapModel, LoadCase);
                StructureResults.FrameResults = frameResults;
            }
            return StructureResults;
        }

        public static List<double> DecomposeResults(Analysis AnalysisResults, string ForceType, string loadcase, int FrameID)
        {

            FrameID -= 1; // SAP starts numbering elements by 1, but the first dictionary in the list is in index 0

            List<double> Forces = new List<double>();
            foreach (FrameAnalysisData fad in AnalysisResults.FrameResults[FrameID].Results[loadcase].Values)
            {
                if (ForceType == "Axial") //Get Axial Forces P
                {
                    Forces.Add(fad.P);
                }
                else if (ForceType == "Shear22") // Get Shear V2
                {
                    Forces.Add(fad.V2);
                }
                else if (ForceType == "Shear33") // Get Shear V3
                {
                    Forces.Add(fad.V3);
                }

                else if (ForceType == "Torsion") // Get Torsion T
                {
                    Forces.Add(fad.T);
                }

                else if (ForceType == "Moment22") // Get Moment M2
                {
                    Forces.Add(fad.M2);
                }
                else if (ForceType == "Moment33") // Get Moment M3
                {
                    Forces.Add(fad.M3);
                }
            }

            return Forces;
        }

        public static List<PolySurface> VisualizeResults(StructuralModel Model, Analysis AnalysisResults, string ForceType, string loadcase, List<int> FrameIDs, double scale)
        {
            List<PolySurface> myVizSurfaces = new List<PolySurface>();
            List<Line> myLines = new List<Line>();
            foreach (int id in FrameIDs)
            {
                int fid = id - 1; // SAP starts numbering elements by 1, but the first dictionary in the list is in index 0

                List<Curve> crossSections = new List<Curve>();
                // get the frame's curve specified by the frameID
                Frame f = (Frame)Model.Frames[fid];
                Curve c = f.BaseCrv;
               
                //CREATE LOCAL COORDINATE SYSTEM
                Vector xAxis = c.TangentAtParameter(0.0);
                Vector yAxis = c.NormalAtParameter(0.0);
                //This ensures the right axis for the Z direction  
                CoordinateSystem localCS = CoordinateSystem.ByOriginVectors(c.StartPoint, xAxis, yAxis);

                //TEST TO VISUALIZE NORMALS
                //Point pt = c.PointAtParameter(0.5);
                //Line ln = Line.ByStartPointDirectionLength(pt, localCS.ZAxis, 30.0);
                //myLines.Add(ln);

                foreach (double t in AnalysisResults.FrameResults[fid].Results[loadcase].Keys)
                {
                    List<Point> crossSectionPoints = new List<Point>();
                    Point fp = c.PointAtParameter(t);
                    Point vp = null;

                    double translateCoord = 0.0;

                    if (ForceType == "Axial") // Get Axial P
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].P * scale;
                    }

                    else if (ForceType == "Shear22") // Get Shear V2
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].V2 * scale;
                    }
                    else if (ForceType == "Shear33") // Get Shear V3
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].V3 * scale;
                    }

                    else if (ForceType == "Torsion") // Get Torsion T
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].T * scale;
                    }

                    else if (ForceType == "Moment22") // Get Moment M2
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].M2 * scale;
                    }

                    else if (ForceType == "Moment33") // Get Moment M3
                    {
                        translateCoord = AnalysisResults.FrameResults[fid].Results[loadcase][t].M3 * scale;
                    }


                    //vp = fp.Add(Vector.ByCoordinates(0.0, 0.0, translateCoord));
                    vp = (Point)fp.Translate(localCS.ZAxis, translateCoord);


                    crossSectionPoints.Add(fp);
                    crossSectionPoints.Add(vp);

                    PolyCurve cc = null;
                    try
                    {
                        cc= PolyCurve.ByPoints(crossSectionPoints);
                    }
                    catch (Exception)
                    {
                        
                        //throw;
                    }
                    

                    crossSections.Add(cc);
                }

                PolySurface loftSurface = PolySurface.ByLoft(crossSections);
                myVizSurfaces.Add(loftSurface);
            }
            return myVizSurfaces;
            
        }

        //Results private methods
        private Analysis() { }
        private Analysis(List<FrameResults> fresults)
        {
            FrameResults = fresults;

        }



    }


}
