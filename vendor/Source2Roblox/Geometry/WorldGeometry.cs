using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Source2Roblox.FileSystem;
using Source2Roblox.Octree;
using Source2Roblox.Textures;
using Source2Roblox.World;
using Source2Roblox.World.Types;

using RobloxFiles.DataTypes;

namespace Source2Roblox.Geometry
{
    public class WorldGeometry
    {
        private const TextureFlags IGNORE = TextureFlags.Hint | TextureFlags.Sky | TextureFlags.Skip | TextureFlags.Trigger;

        public readonly Dictionary<string, ValveMaterial> Materials = new Dictionary<string, ValveMaterial>();
        public readonly List<HashSet<Face>> FaceClusters = new List<HashSet<Face>>();

        public readonly StaticProps StaticProps = null;
        public readonly DetailProps DetailProps = null;

        public readonly ObjMesh Mesh = new ObjMesh(1f / Program.STUDS_TO_VMF);
        public readonly StringBuilder MtlBuilder = new StringBuilder();

        public readonly BSPFile Level;
        public readonly string ExportDir;

        public ValveMaterial RegisterMaterial(string material, GameMount game = null, Face face = null)
        {
            string mapName = Level.Name;

            if (material.StartsWith($"maps/{mapName}"))
            {
                while (true)
                {
                    var lastUnderscore = material.LastIndexOf('_');

                    if (lastUnderscore < 0)
                        break;

                    string number = material.Substring(lastUnderscore + 1);

                    if (int.TryParse(number, out int _))
                    {
                        material = material.Substring(0, lastUnderscore);
                        continue;
                    }

                    material = material.Replace($"maps/{mapName}/", "");
                    break;
                }
            }

            if (material.EndsWith("_wvt_patch"))
                material = material.Replace("_wvt_patch", "");

            if (face != null)
                face.Material = material;

            bool skip = false;

            if (material.ToLowerInvariant() == "tools/toolstrigger")
                skip = true;

            if (material.Contains("water") && material.Contains("_beneath"))
                skip = true;

            if (skip)
            {
                if (face != null)
                    face.Skip = true;

                return null;
            }

            if (!Materials.ContainsKey(material))
            {
                Console.WriteLine($"Fetching material {material}");

                var vmt = new ValveMaterial(material, game);
                Mesh.WriteLine_MTL($"newmtl", material);
                Materials[material] = vmt;

                string diffuse = vmt.DiffusePath;
                string bump = vmt.BumpPath;

                if (!string.IsNullOrEmpty(diffuse))
                {
                    string png = diffuse.Replace(".vtf", ".png");
                    Mesh.WriteLine_MTL("\tmap_Kd", png);
                    if (!string.IsNullOrEmpty(ExportDir))
                        vmt.SaveVTF(diffuse, ExportDir);
                }

                if (!string.IsNullOrEmpty(bump))
                {
                    string png = bump.Replace(".vtf", ".png");
                    Mesh.WriteLine_MTL($"\tbump", png);
                    if (!string.IsNullOrEmpty(ExportDir))
                        vmt.SaveVTF(bump, ExportDir);
                }
            }

            if (Materials.TryGetValue(material, out ValveMaterial result))
                return result;

            return null;
        }

        public WorldGeometry(BSPFile bsp, string exportDir, GameMount game = null)
        {
            string mapName = bsp.Name;

            var edges = bsp.Edges;
            var surfEdges = bsp.SurfEdges;

            var vertIndices = new List<uint>();
            var normIndices = bsp.VertNormalIndices;

            foreach (int surf in surfEdges)
            {
                uint edge = 0;

                if (surf >= 0)
                {
                    int edgeIdx = surf * 2;
                    if (edgeIdx >= 0 && edgeIdx < edges.Count)
                        edge = edges[edgeIdx];
                }
                else
                {
                    int edgeIdx = -surf * 2 + 1;
                    if (edgeIdx >= 0 && edgeIdx < edges.Count)
                        edge = edges[edgeIdx];
                }

                vertIndices.Add(edge);
            }

            Level = bsp;
            ExportDir = exportDir;

            var vertOffsets = new Dictionary<int, Vector3>();
            var brushModels = bsp.BrushModels;

            foreach (var entity in bsp.Entities)
            {
                string className = entity.ClassName;
                string modelId = entity.Get<string>("model");

                if (modelId == null)
                    continue;

                if (!modelId.StartsWith("*"))
                    continue;

                if (!int.TryParse(modelId.Substring(1), out int modelIndex))
                    continue;

                if (modelIndex < 0 || modelIndex >= brushModels.Count)
                    continue;

                bool shouldSkip = false;

                if (className.StartsWith("trigger"))
                    shouldSkip = true;

                var model = brushModels[modelIndex];
                var origin = entity.Get<Vector3>("origin");

                if (origin == null)
                    continue;

                var offset = origin - model.Origin;
                int faceIndex = model.FirstFace;
                int numFaces = model.NumFaces;

                for (int i = 0; i < numFaces; i++)
                {
                    int faceId = faceIndex + i;
                    if (faceId < 0 || faceId >= bsp.Faces.Count)
                        continue;

                    var face = bsp.Faces[faceId];

                    if (shouldSkip)
                    {
                        face.Skip = true;
                        continue;
                    }

                    var firstEdge = face.FirstEdge;
                    var numEdges = face.NumEdges;

                    for (int j = 0; j < numEdges; j++)
                    {
                        int edgeIdx = firstEdge + j;
                        if (edgeIdx >= 0 && edgeIdx < vertIndices.Count)
                        {
                            var vertId = (int)vertIndices[edgeIdx];
                            vertOffsets[vertId] = offset;
                        }
                    }

                    face.Entity = entity;
                }

                model.Origin = origin;
                model.BoundsMin += offset;
                model.BoundsMax += offset;
            }

            int numVerts = 0;
            int numNorms = 0;
            int numUVs = 0;

            for (int i = 0; i < bsp.Faces.Count; i++)
            {
                var face = bsp.Faces[i];
                face.FaceIndex = i;

                var numEdges = face.NumEdges;
                face.FirstNorm = numNorms;

                string matName = null;
                if (face.TexInfo >= 0 && face.TexInfo < bsp.TexInfo.Count)
                {
                    var texInfo = bsp.TexInfo[face.TexInfo];
                    if (texInfo.TextureData >= 0 && texInfo.TextureData < bsp.TexData.Count)
                    {
                        var texData = bsp.TexData[texInfo.TextureData];
                        int stringIndex = texData.StringTableIndex;
                        if (stringIndex >= 0 && stringIndex < bsp.TexDataStringTable.Count)
                        {
                            var matIndex = bsp.TexDataStringTable[stringIndex];
                            bsp.TexDataStringData.TryGetValue(matIndex, out matName);
                        }
                    }
                }

                face.Material = matName ?? "tools/toolsnodraw";
                numNorms += numEdges;
            }

            var faces = bsp.Faces.OrderBy(face => face.Material);
            var faceOctree = new Octree<Face>();
            numNorms = 0;

            foreach (Face face in faces)
            {
                int planeId = face.PlaneNum;
                int dispInfo = face.DispInfo;
                int numEdges = face.NumEdges;

                int firstEdge = face.FirstEdge;
                int firstNorm = face.FirstNorm;

                Texel texel = null;
                Vector2 size = new Vector2(32, 32);
                TextureFlags flags = TextureFlags.None;

                if (face.TexInfo >= 0 && face.TexInfo < bsp.TexInfo.Count)
                {
                    var texInfo = bsp.TexInfo[face.TexInfo];
                    texel = texInfo.TextureVecs;
                    flags = texInfo.Flags;

                    if (texInfo.TextureData >= 0 && texInfo.TextureData < bsp.TexData.Count)
                    {
                        var texData = bsp.TexData[texInfo.TextureData];
                        size = texData.Size;
                    }
                }

                var material = face.Material;

                if ((flags & IGNORE) != TextureFlags.None)
                {
                    face.Skip = true;
                    continue;
                }

                var center = new Vector3();
                var vmt = RegisterMaterial(material, game, face);

                if (face.Skip)
                    continue;

                if (face.DispInfo >= 0)
                {
                    if (dispInfo < 0 || dispInfo >= bsp.Displacements.Count)
                    {
                        face.Skip = true;
                        continue;
                    }

                    var disp = bsp.Displacements[dispInfo];
                    Debug.Assert(face.NumEdges == 4);

                    var corners = new Vector3[4];
                    var startPos = disp.StartPosition;

                    var bestDist = float.MaxValue;
                    var bestIndex = -1;

                    for (int i = 0; i < 4; i++)
                    {
                        int edgeIdx = firstEdge + i;
                        Vector3 vertex = new Vector3();
                        if (edgeIdx >= 0 && edgeIdx < vertIndices.Count)
                        {
                            var vertIndex = (int)vertIndices[edgeIdx];
                            if (vertIndex >= 0 && vertIndex < bsp.Vertices.Count)
                                vertex = bsp.Vertices[vertIndex];
                        }

                        var dist = (vertex - startPos).Magnitude;
                        corners[i] = vertex;

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestIndex = i;
                        }
                    }

                    if (bestIndex != 0)
                    {
                        var leftHalf = corners.Skip(bestIndex);
                        var rightHalf = corners.Take(bestIndex);

                        var slice = leftHalf.Concat(rightHalf);
                        corners = slice.ToArray();
                    }

                    var dispSize = (1 << disp.Power) + 1;
                    var faceNormal = new Vector3();

                    Vector3 defaultPlaneNorm = new Vector3(0, 1, 0);
                    if (planeId >= 0 && planeId < bsp.Planes.Count)
                    {
                        var pn = bsp.Planes[planeId].Normal;
                        if (face.Side != 0)
                            pn = -pn;
                        defaultPlaneNorm = new Vector3(pn.X, pn.Z, -pn.Y);
                    }

                    for (int i = 0; i < numEdges; i++)
                    {
                        Vector3 norm = defaultPlaneNorm;
                        int normIdxPos = firstNorm + i;
                        if (normIdxPos >= 0 && normIdxPos < normIndices.Count)
                        {
                            var normIndex = (int)normIndices[normIdxPos];
                            if (normIndex >= 0 && normIndex < bsp.VertNormals.Count)
                            {
                                var rawNorm = bsp.VertNormals[normIndex];
                                norm = new Vector3(rawNorm.X, rawNorm.Z, -rawNorm.Y);
                            }
                        }

                        faceNormal += norm;
                    }

                    faceNormal = (faceNormal.Magnitude > 0) ? faceNormal.Unit : defaultPlaneNorm;
                    numEdges = (dispSize * dispSize);

                    var alpha = 0f;

                    for (int y = 0; y < dispSize; y++)
                    {
                        var rowSample = (float)y / (dispSize - 1);
                        var rowStart = corners[0].Lerp(corners[1], rowSample);
                        var rowEnd = corners[3].Lerp(corners[2], rowSample);

                        for (int x = 0; x < dispSize; x++)
                        {
                            var colSample = (float)x / (dispSize - 1);
                            var i = disp.DispVertStart + (y * dispSize) + x;

                            var pos = rowStart.Lerp(rowEnd, colSample);
                            var uv = (texel != null) ? texel.CalcTexCoord(pos, size) : new Vector2();

                            if (i >= 0 && i < bsp.DispVerts.Count)
                            {
                                var dispVert = bsp.DispVerts[i];
                                pos += dispVert.Vector * dispVert.Dist;
                                alpha += dispVert.Alpha / numEdges;
                            }

                            center += pos / numEdges;

                            Mesh.AddVertex(pos.X, pos.Z, -pos.Y);
                            Mesh.AddUV(uv.X, 1f - uv.Y);
                        }
                    }

                    var normalSamples = new Dictionary<int, Vector3>();
                    var meshVerts = Mesh.Vertices;

                    var preSample = new Action<int>((index) =>
                    {
                        if (!normalSamples.TryGetValue(index, out var sampler))
                        {
                            sampler = new Vector3();
                            normalSamples[index] = sampler;
                        }
                    });

                    for (int y = 0; y < dispSize - 1; y++)
                    {
                        for (int x = 0; x < dispSize - 1; x++)
                        {
                            int aa = numVerts + (y * dispSize) + x;
                            preSample(aa);

                            int ab = aa + 1;
                            preSample(ab);

                            int bb = ab + dispSize;
                            preSample(bb);

                            int ba = bb - 1;
                            preSample(ba);

                            Vector3 a = (aa >= 0 && aa < meshVerts.Count) ? meshVerts[aa] : new Vector3(),
                                    b = (ab >= 0 && ab < meshVerts.Count) ? meshVerts[ab] : new Vector3(),
                                    c = (ba >= 0 && ba < meshVerts.Count) ? meshVerts[ba] : new Vector3(),
                                    d = (bb >= 0 && bb < meshVerts.Count) ? meshVerts[bb] : new Vector3();

                            var normA = (a - c).Cross(d - c).Unit;
                            var normB = (a - d).Cross(b - d).Unit;

                            var dotA = faceNormal.Dot(normA);
                            var dotB = faceNormal.Dot(normB);

                            if (dotA < 0)
                                normA = -normA;

                            if (dotB < 0)
                                normB = -normB;

                            normalSamples[aa] += normA;
                            normalSamples[ba] += normA;
                            normalSamples[bb] += normA;

                            normalSamples[aa] += normB;
                            normalSamples[bb] += normB;
                            normalSamples[ab] += normB;
                        }
                    }

                    for (int i = 0; i < numEdges; i++)
                        if (normalSamples.TryGetValue(numVerts + i, out var samples))
                            Mesh.AddNormal(samples.Unit);

                    Mesh.SetObject($"disp_{face.DispInfo}");
                    face.NumEdges = (short)numEdges;
                    face.Area = 0;
                }
                else
                {
                    Vector3 defaultPlaneNorm = new Vector3(0, 1, 0);
                    if (planeId >= 0 && planeId < bsp.Planes.Count)
                    {
                        var pn = bsp.Planes[planeId].Normal;
                        if (face.Side != 0)
                            pn = -pn;
                        defaultPlaneNorm = new Vector3(pn.X, pn.Z, -pn.Y);
                    }

                    for (int i = numEdges - 1; i >= 0; i--)
                    {
                        int edgeIdx = firstEdge + i;
                        int vertIndex = (edgeIdx >= 0 && edgeIdx < vertIndices.Count) ? (int)vertIndices[edgeIdx] : -1;
                        var vert = (vertIndex >= 0 && vertIndex < bsp.Vertices.Count) ? bsp.Vertices[vertIndex] : new Vector3();

                        Vector3 norm = defaultPlaneNorm;
                        int normIdxPos = firstNorm + i;
                        if (normIdxPos >= 0 && normIdxPos < normIndices.Count)
                        {
                            var normIndex = (int)normIndices[normIdxPos];
                            if (normIndex >= 0 && normIndex < bsp.VertNormals.Count)
                            {
                                var rawNorm = bsp.VertNormals[normIndex];
                                norm = new Vector3(rawNorm.X, rawNorm.Z, -rawNorm.Y);
                            }
                        }

                        var uv = (texel != null) ? texel.CalcTexCoord(vert, size) : new Vector2();

                        if (vertIndex >= 0 && vertOffsets.ContainsKey(vertIndex))
                            vert += vertOffsets[vertIndex];

                        center += vert / numEdges;
                        Mesh.AddUV(uv.X, uv.Y);
                        Mesh.AddVertex(vert.X, vert.Z, -vert.Y);
                        Mesh.AddNormal(norm.X, norm.Y, norm.Z);
                    }
                }

                face.Center = center;
                face.FirstUV = numUVs;
                face.FirstNorm = numNorms;
                face.FirstVert = numVerts;

                numUVs += numEdges;
                numNorms += numEdges;
                numVerts += numEdges;

                faceOctree.CreateNode(center, face);
            }

            var facesLeft = faces
                .Where(face => !face.Skip)
                .OrderBy(face => face.FaceIndex)
                .ToList();

            int faceTotal = facesLeft.Count,
                faceCount = 0;

            while (facesLeft.Any())
            {
                Console.WriteLine($"Clumping faces... {faceCount}/{faceTotal}");
                Program.Emit("progress", $"Clumping faces {faceCount}/{faceTotal}.", new
                {
                    phase = "clumping",
                    current = faceCount,
                    total = faceTotal
                });

                var face = facesLeft.First();
                facesLeft.Remove(face);

                var area = face.Area;
                var sqrtArea = Math.Sqrt(area);

                var searchArea = (int)Math.Max(4000, sqrtArea * 5);
                var material = face.Material.ToLowerInvariant();
                var cluster = new HashSet<Face>() { face };

                if (!material.StartsWith("lights/white"))
                {
                    var nearby = faceOctree
                        .RadiusSearch(face.Center, searchArea)
                        .Where(other => other.Material == face.Material)
                        .Where(other => other.Entity == face.Entity)
                        .Where(facesLeft.Contains)
                        .OrderBy(other => other.FaceIndex);

                    foreach (var other in nearby)
                    {
                        facesLeft.Remove(other);
                        cluster.Add(other);
                    }
                }

                FaceClusters.Add(cluster);
                faceCount += cluster.Count;
            }

            var staticProps = bsp.FindGameLump("sprp");
            var detailProps = bsp.FindGameLump("dprp");
            
            if (staticProps != null)
            {
                using (var propStream = new MemoryStream(staticProps.Content))
                using (var reader = new BinaryReader(propStream))
                {
                    var props = new StaticProps(bsp, staticProps, reader);
                    StaticProps = props;
                }
            }
            
            if (detailProps != null)
            {
                using (var propStream = new MemoryStream(detailProps.Content))
                using (var reader = new BinaryReader(propStream))
                {
                    var props = new DetailProps(reader);
                    DetailProps = props;
                }
            }
        }
    }
}
