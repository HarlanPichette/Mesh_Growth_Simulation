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
    public class MeshAgent
    {
        public bool iGrow;
        public int iVertexCount;
        public double iCollisionRange;
        public double iMaxHeight;
        public double iVerticalWeight;
        public double iUniformWeight;
        public PlanktonMesh PtMesh;
        public List<Point3d> iFacadeVertexes;
        public RTree iFacadeRTree;
        public RTree LocalRtree;
        private List<Vector3d> totalWeightedMoves;
        private List<double> totalWeights;
        private List<int> nakedVertexes;

        public Point3d AveragePosition;

        public double Datum = 0.0;

        public MeshAgent(Mesh startingMesh) { PtMesh = startingMesh.ToPlanktonMesh(); }

        public void Update()
        {
            UpdateLocalRtree();
            CreateWeights();
            GetNakedVertexes();
            Uniform();
            Vertical();
            CollisionDetection();
            EdgeLength();
            UpdateVertices();
            Growth();
        }

        private void GetNakedVertexes()
        {
            nakedVertexes = new List<int>();

            for (int i = 0; i < PtMesh.Halfedges.Count; i++)
            {
                int outAdjacentFace = PtMesh.Halfedges[i].AdjacentFace;
                if (outAdjacentFace == -1)
                {
                    int start = PtMesh.Halfedges[i].StartVertex;
                    nakedVertexes.Add(start);
                }
            }
        }

        private void UpdateLocalRtree()
        {
            AveragePosition = new Point3d();
            LocalRtree = new RTree();
            for (int i = 0; i < PtMesh.Vertices.Count; i++)
            {
                AveragePosition += PtMesh.Vertices[i].ToPoint3d();
                LocalRtree.Insert(PtMesh.Vertices[i].ToPoint3d(), i);
            }
            AveragePosition = AveragePosition / PtMesh.Vertices.Count;
        }

        private void CreateWeights()
        {
            totalWeightedMoves = new List<Vector3d>();
            totalWeights = new List<double>();
            AveragePosition = new Point3d();

            for (int i = 0; i < PtMesh.Vertices.Count; i++)
            {
                totalWeightedMoves.Add(new Vector3d(0, 0, 0));
                totalWeights.Add(0);
                AveragePosition += PtMesh.Vertices[i].ToPoint3d();
            }

            AveragePosition = AveragePosition / PtMesh.Vertices.Count;

        }

        public void Uniform()
        {
            if (iUniformWeight > 0) {
                double sumNakedZ = 0.0;
                double averageNakedZ = 0.0;

                for (int i = 0; i < nakedVertexes.Count; i++)
                {
                    Point3d thisPoint = PtMesh.Vertices[nakedVertexes[i]].ToPoint3d();
                    sumNakedZ += thisPoint.Z;
                }
                averageNakedZ = sumNakedZ / nakedVertexes.Count;

                for (int i = 0; i < nakedVertexes.Count; i++)
                {
                    Point3d thisPoint = PtMesh.Vertices[nakedVertexes[i]].ToPoint3d();
                    double difference = averageNakedZ - thisPoint.Z;
                    Vector3d uniform = new Vector3d(0, 0, difference);
                    totalWeightedMoves[nakedVertexes[i]] += uniform * iUniformWeight;
                    totalWeights[nakedVertexes[i]] += iUniformWeight;
                }
            }
        }

        public void Vertical()
        {
            if (iVerticalWeight > 0)
            {

                for (int i = 0; i < nakedVertexes.Count; i++)
                {
                    double Magnitude;
                    Point3d thisPoint = PtMesh.Vertices[nakedVertexes[i]].ToPoint3d();

                    if (thisPoint.Z > 0.9 * iMaxHeight) Magnitude = (0.05 * iMaxHeight) * ((iMaxHeight - thisPoint.Z) / (iMaxHeight - iMaxHeight * 0.9));
                    else Magnitude = (0.05 * iMaxHeight);

                    Vector3d move = new Vector3d(0, 0, Magnitude);
                    totalWeightedMoves[nakedVertexes[i]] += move * iVerticalWeight;
                    totalWeights[nakedVertexes[i]] += iVerticalWeight;
                }
            }
        }

        private void CollisionDetection()
        {

            //add the attraction to facade vertexes

            for (int i = 0; i < PtMesh.Vertices.Count; i++)
            {
                Point3d thisVertex = PtMesh.Vertices[i].ToPoint3d();


                //repel from itself

                Sphere searchSphere = new Sphere(thisVertex, iCollisionRange);

                //lowering the weight of the collision to less than the facade repel, results in less intersection
                LocalRtree.Search(searchSphere, (sender, args) =>
                {
                    if (i > args.Id)
                    {
                        Point3d returnedVertex = PtMesh.Vertices[args.Id].ToPoint3d();
                        Vector3d direction = returnedVertex - thisVertex;
                        double distance = direction.Length;
                        direction.Unitize();
                        direction = direction * (distance - iCollisionRange);
                        totalWeightedMoves[i] += direction * 0.1;
                        totalWeights[i] += 0.1;
                        totalWeightedMoves[args.Id] -= direction * 0.1;
                        totalWeights[args.Id] += 0.1;
                    }
                });
            }

            if (iFacadeVertexes != null)
            {
                for (int i = 0; i < PtMesh.Vertices.Count; i++)
                {
                    Point3d thisVertex = PtMesh.Vertices[i].ToPoint3d();

                    Sphere searchSphere = new Sphere(thisVertex, iCollisionRange);

                    iFacadeRTree.Search(searchSphere, (sender, args) =>
                    {
                        Point3d returnedVertex = iFacadeVertexes[args.Id];
                        Vector3d direction = returnedVertex - thisVertex;
                        double distance = direction.Length;
                        direction.Unitize();
                        direction = direction * (distance - iCollisionRange);
                        totalWeightedMoves[i] += direction;
                        totalWeights[i] += 1;
                    });
                }
            }
        }

        private void EdgeLength()
        {
            for (int i = 0; i < PtMesh.Halfedges.Count; i += 2)
            {
                int startVertex = PtMesh.Halfedges[i].StartVertex;
                int endVertex = PtMesh.Halfedges.EndVertex(i);

                Vector3d direction = PtMesh.Vertices[endVertex].ToPoint3d() - PtMesh.Vertices[startVertex].ToPoint3d();
                double distance = direction.Length;

                if (distance > iCollisionRange)
                {
                    direction.Unitize();
                    direction = direction * ((distance - iCollisionRange) / 2);
                    totalWeightedMoves[startVertex] += direction * 0.5;
                    totalWeights[startVertex] += 0.5;
                    totalWeightedMoves[endVertex] -= direction;
                    totalWeights[endVertex] += 0.5;
                }
            }
        }

        private void UpdateVertices()
        {
            for (int i = 0; i < PtMesh.Vertices.Count; i++)
            {
                if (totalWeights[i] == 0) { continue; }
                else
                {
                    if (Datum > 0.0)
                    {
                        Vector3d move = totalWeightedMoves[i] / totalWeights[i];
                        Point3d newPosition = PtMesh.Vertices[i].ToPoint3d() + move;
                        PtMesh.Vertices.SetVertex(i, newPosition.X, newPosition.Y, Datum);
                    }
                    else
                    {
                        Vector3d move = totalWeightedMoves[i] / totalWeights[i];
                        Point3d newPosition = PtMesh.Vertices[i].ToPoint3d() + move;
                        PtMesh.Vertices.SetVertex(i, newPosition.X, newPosition.Y, newPosition.Z);
                    }



                }
            }
        }

        private void Growth()
        {

            for (int i = 0; i < PtMesh.Halfedges.Count; i += 2)
            {
                int startVertex = PtMesh.Halfedges[i].StartVertex;
                int endVertex = PtMesh.Halfedges.EndVertex(i);

                Vector3d direction = PtMesh.Vertices[endVertex].ToPoint3d() - PtMesh.Vertices[startVertex].ToPoint3d();
                double distance = direction.Length;

                if (distance > iCollisionRange) SplitLongEdge(i, iCollisionRange, iVertexCount);
            }
        }

        private void SplitLongEdge(int index, double splitLength, int vertexCount)
        {
            if (iGrow && PtMesh.Vertices.Count < vertexCount)
            {
                double length = PtMesh.Halfedges.GetLength(index);
                if (length >= splitLength * 0.99) SplitEdge(index);
            }
        }

        private void SplitEdge(int edgeIndex)
        {
            int newHalfEdgeIndex = PtMesh.Halfedges.SplitEdge(edgeIndex);

            PtMesh.Vertices.SetVertex(
                PtMesh.Vertices.Count - 1,
                0.5 * (PtMesh.Vertices[PtMesh.Halfedges[edgeIndex].StartVertex].ToPoint3d() + PtMesh.Vertices[PtMesh.Halfedges[edgeIndex + 1].StartVertex].ToPoint3d()));

            if (PtMesh.Halfedges[edgeIndex].AdjacentFace >= 0)
                PtMesh.Faces.SplitFace(newHalfEdgeIndex, PtMesh.Halfedges[edgeIndex].PrevHalfedge);

            if (PtMesh.Halfedges[edgeIndex + 1].AdjacentFace >= 0)
                PtMesh.Faces.SplitFace(edgeIndex + 1, PtMesh.Halfedges[PtMesh.Halfedges[edgeIndex + 1].NextHalfedge].NextHalfedge);
        }

    }
}
