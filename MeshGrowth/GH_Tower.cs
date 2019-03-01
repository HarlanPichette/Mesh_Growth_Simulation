using System;
using System.Collections.Generic;
using System.Linq;
using Plankton;
using PlanktonGh;
using Grasshopper.Kernel;
using Rhino.Geometry;



namespace MeshGrowth
{
    public class GH_Tower : GH_Component
    {

        private CollectionMeshAgents meshAgents;
        private int iteration = 0;

        public GH_Tower()
            : base("MeshGrowthTower", "MeshGrowthTower", "MeshGrowthTower", "McMuffin", "MeshGrowthTower")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Grow", "Grow", "Grow", GH_ParamAccess.item);

            pManager.AddNumberParameter("FacadeVertexCount", "FacadeVertexCount", "FacadeVertexCount", GH_ParamAccess.item);
            pManager.AddNumberParameter("SlabVertexCount", "SlabVertexCount", "SlabVertexCount", GH_ParamAccess.item);
            pManager.AddNumberParameter("CollisionRange", "CollisionRange", "CollisionRange", GH_ParamAccess.item);
            pManager.AddNumberParameter("FloortoFloor", "FloortoFloor", "FloortoFloor", GH_ParamAccess.item);
            pManager.AddNumberParameter("CeilingOffset", "CeilingOffset", "CeilingOffset", GH_ParamAccess.item);
            pManager.AddNumberParameter("MaxHeight", "MaxHeight", "MaxHeight", GH_ParamAccess.item);
            pManager.AddNumberParameter("VerticalWeight", "VerticalWeight", "VerticalWeight", GH_ParamAccess.item);
            pManager.AddNumberParameter("UniformWeight", "UniformWeight", "UniformWeight", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Iterations", "Iterations", "Iterations", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh iMesh = new Mesh();
            bool iReset = false;
            bool iGrow = false;  
            double iFacadeVertexCount = 0.0;
            double iSlabVertexCount = 0.0;
            double iCollisionRange = 0.0;
            double iFloortoFloor = 0.0;
            double iCeilingOffset = 0.0;
            double iMaxHeight = 0.0;
            double iVerticalWeight = 0.0;
            double iUniformWeight = 0.0;

            DA.GetData("Mesh", ref iMesh);
            DA.GetData("Reset", ref iReset);
            DA.GetData("Grow", ref iGrow);
            DA.GetData("FacadeVertexCount", ref iFacadeVertexCount);
            DA.GetData("SlabVertexCount", ref iSlabVertexCount);
            DA.GetData("CollisionRange", ref iCollisionRange);
            DA.GetData("FloortoFloor", ref iFloortoFloor);
            DA.GetData("CeilingOffset", ref iCeilingOffset);
            DA.GetData("MaxHeight", ref iMaxHeight);
            DA.GetData("VerticalWeight", ref iVerticalWeight);
            DA.GetData("UniformWeight", ref iUniformWeight);

            if (meshAgents == null || iReset) { meshAgents = new CollectionMeshAgents(iMesh); iteration = 0; }

            meshAgents.iGrow = iGrow;
            meshAgents.iFacadeVertexCount = (int)iFacadeVertexCount;
            meshAgents.iSlabVertexCount = (int)iSlabVertexCount;
            meshAgents.iCollisionRange = iCollisionRange;
            meshAgents.iFloortoFloor = iFloortoFloor;
            meshAgents.iCeilingOffset = iCeilingOffset;
            meshAgents.iMaxHeight = iMaxHeight;
            meshAgents.iVerticalWeight = iVerticalWeight;
            meshAgents.iUniformWeight = iUniformWeight;

            meshAgents.UpdateCollection();
            iteration++;

            DA.SetDataList("Mesh", meshAgents.GetRhinoGeometry());
            DA.SetData("Iterations", iteration);
        }

        protected override System.Drawing.Bitmap Icon { get { return null; } }

        public override Guid ComponentGuid { get { return new Guid("bb7c9f9c-a901-47b1-b0e5-fec9cb7562bf"); } }
    }
}