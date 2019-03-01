using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Plankton;
using PlanktonGh;
using Rhino.Geometry;

namespace MeshGrowth
{
    //class
    class CollectionMeshAgents
    {
        public bool iGrow;
        public int iFacadeVertexCount;
        public int iSlabVertexCount;
        public double iCollisionRange;
        public double iFloortoFloor;
        public double iCeilingOffset;
        public double iMaxHeight;
        public double iVerticalWeight;
        public double iUniformWeight;

        private List<Point3d> iFacadeVertexes;
        private RTree iFacadeRTree;

        private bool initialize = true;
        private List<bool> newLevels = new List<bool>();
        private List<double> floorlevels = new List<double>();
        private List<bool> newCeilingLevels = new List<bool>();
        private List<double> ceilinglevels = new List<double>();


        public List<MeshAgent> meshBodies;

        public CollectionMeshAgents(Mesh iMesh)
        {
            meshBodies = new List<MeshAgent>();
            meshBodies.Add(new MeshAgent(iMesh));
        }

        public void UpdateCollection()
        {
            AllVertexes();
            SetValues();
            UpdateAgentBodies();
            CreateSlab();
        }

        public void AllVertexes() {

            iFacadeVertexes = new List<Point3d>();
            iFacadeRTree = new RTree();

            for (int i = 0; i < meshBodies[0].PtMesh.Vertices.Count; i++){
                iFacadeVertexes.Add(meshBodies[0].PtMesh.Vertices[i].ToPoint3d());
                iFacadeRTree.Insert(meshBodies[0].PtMesh.Vertices[i].ToPoint3d(), i);
            }
        }

        private void SetValues()
        {

            meshBodies[0].iGrow = iGrow;
            meshBodies[0].iVertexCount = (int)iFacadeVertexCount;
            meshBodies[0].iCollisionRange = iCollisionRange;
            meshBodies[0].iMaxHeight = iMaxHeight;
            meshBodies[0].iVerticalWeight = iVerticalWeight;
            meshBodies[0].iUniformWeight = iUniformWeight;

            for (int i = 1; i < meshBodies.Count; i++) {
                meshBodies[i].iGrow = iGrow;
                meshBodies[i].iVertexCount = (int)iSlabVertexCount;
                meshBodies[i].iCollisionRange = iCollisionRange;
                meshBodies[i].iFacadeVertexes = iFacadeVertexes;
                meshBodies[i].iFacadeRTree = iFacadeRTree;
            }
        }

        private void UpdateAgentBodies() { for (int i = 0; i < meshBodies.Count; i++) meshBodies[i].Update(); }

        public List<Mesh> GetRhinoGeometry() {
            List<Mesh> rhinoGeometry = new List<Mesh>();

            for (int i = 0; i < meshBodies.Count; i++) { rhinoGeometry.Add(meshBodies[i].PtMesh.ToRhinoMesh()); }

            return rhinoGeometry;
        }

        private void CreateSlab()
        {

            if (iMaxHeight > 0 && iFloortoFloor > 0)
            {

                int floorCount = (int)(iMaxHeight / iFloortoFloor);

                if (initialize)
                {
                    for (int i = 1; i < floorCount; i++)
                    {

                        ceilinglevels.Add((i * iFloortoFloor) - iCeilingOffset);
                        newCeilingLevels.Add(true);
                        floorlevels.Add(i * iFloortoFloor);
                        newLevels.Add(true);
                    }
                    initialize = false;
                }

                for (int i = 0; i < newLevels.Count; i++)
                {
                    List<int> collisionIndices = new List<int>();
                    BoundingBox searchBox = new BoundingBox(-500, -500, floorlevels[i], 500, 500, floorlevels[i] + iFloortoFloor * 0.99);
                    meshBodies[0].LocalRtree.Search(searchBox, (sender, args) => { collisionIndices.Add(args.Id); });

                    if (collisionIndices.Count > iFacadeVertexCount * 0.05 && newLevels[i])
                    {

                        MeshAgent cb = CreateBody(new Point3d(meshBodies[0].AveragePosition.X, meshBodies[0].AveragePosition.Y, ceilinglevels[i]));
                        cb.Datum = ceilinglevels[i];
                        newCeilingLevels[i] = false;
                        meshBodies.Add(cb);

                        MeshAgent fb = CreateBody(new Point3d(meshBodies[0].AveragePosition.X, meshBodies[0].AveragePosition.Y, floorlevels[i]));
                        fb.Datum = floorlevels[i];
                        newLevels[i] = false;
                        meshBodies.Add(fb);
                    }
                }
            }
        }

        private MeshAgent CreateBody(Point3d startPoint)
        {
            Mesh newBody = new Mesh();
            newBody.Vertices.Add(startPoint);
            newBody.Vertices.Add(new Point3d(startPoint.X + iCollisionRange, startPoint.Y, startPoint.Z));
            newBody.Vertices.Add(new Point3d(startPoint.X, startPoint.Y + iCollisionRange, startPoint.Z));
            newBody.Faces.AddFace(0, 1, 2);

            return new MeshAgent(newBody);
        }

    }
}
