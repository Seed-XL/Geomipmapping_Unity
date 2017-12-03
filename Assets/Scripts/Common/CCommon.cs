using System;
using UnityEngine; 


namespace Assets.Scripts.Common
{
    #region 纹理数据 

    public enum enTileTypes
    {
        lowest_tile = 0,
        low_tile = 1,
        high_tile = 2,
        highest_tile = 3,
        max_tile = 4,
    }

    public class CTerrainTile
    {
        public int lowHeight;
        public int optimalHeight;
        public int highHeight;
        public enTileTypes TileType;

        public Texture2D mTileTexture;


        public CTerrainTile(enTileTypes tileType, Texture2D texture)
        {
            lowHeight = 0;
            optimalHeight = 0;
            highHeight = 0;

            TileType = tileType;
            mTileTexture = texture;
        }
    }






    #endregion

    #region  高度图数据

    public struct stHeightData
    {
        private ushort[,] mHeightData;
        public int mSize;

        public bool IsValid()
        {
            return mHeightData != null;
        }

        public void Release()
        {
            mHeightData = null;
            mSize = 0;
        }


        public void Allocate(int mapSize)
        {
            if (mapSize > 0)
            {
                mHeightData = new ushort[mapSize, mapSize];
                mSize = mapSize;
            }
        }

        public void SetHeightValue(ushort value, int x, int y)
        {
            if (IsValid() && InRange(x, y))
            {
                mHeightData[x, y] = value;
            }
        }

        public ushort GetRawHeightValue(int x, int y)
        {
            ushort ret = 0;
            if (IsValid() && InRange(x, y))
            {
                ret = mHeightData[x, y];
            }
            return ret;
        }




        private bool InRange(int x, int y)
        {
            return x >= 0 && x < mSize && y >= 0 && y < mSize;
        }
    }

    #endregion


    #region  粗糙度数据 
    public struct stRoughnessData
    {
        private int[,] mRoughnessData;
        public int mSize;

        public bool IsValid()
        {
            return mRoughnessData != null;
        }

        public void Release()
        {
            mRoughnessData = null;
            mSize = 0;
        }


        public void Allocate(int mapSize)
        {
            if (mapSize > 0)
            {
                mRoughnessData = new int[mapSize, mapSize];
                mSize = mapSize;
            }
        }

        public void SetRoughnessValue(int value, int x, int y)
        {
            if (IsValid() && InRange(x, y))
            {
                mRoughnessData[x, y] = value;
            }
        }

        public int GetRoughnessValue(int x, int y)
        {
            int ret = 0;
            if (IsValid() && InRange(x, y))
            {
                ret = mRoughnessData[x, y];
            }
            return ret;
        }


        private bool InRange(int x, int y)
        {
            return x >= 0 && x < mSize && y >= 0 && y < mSize;
        }

        public void Reset( int value = 1 )
        {
            if( IsValid() ) 
            {
                for( int z = 0;  z < mSize; ++z )
                {
                    for(int x = 0; x < mSize; ++x)
                    {
                        mRoughnessData[x, z] = value ;
                    }
                }
            }
        }
    }

    #endregion

    #region Mesh相关

    public struct stVertexAtrribute
    {
        public Vector3 mVertice;
        public Vector2 mUV;
        public int mVerticeIdx;

        public stVertexAtrribute(int vertexIdx, Vector3 vertex, Vector2 uv)
        {
            mVerticeIdx = vertexIdx;
            mVertice = vertex;
            mUV = uv;
        }

        public stVertexAtrribute Clone()
        {
            return new stVertexAtrribute(mVerticeIdx, mVertice, mUV);
        }

    }



    public struct stTerrainMeshData
    {
        public Mesh mMesh;
        public Vector3[] mVertices;
        public Vector2[] mUV;
        public Vector3[] mNormals;
        public int[] mTriangles;


        private int mTriIdx;


        public void RenderVertex(
            int idx,
            Vector3 vertex,
            Vector3 uv
            )
        {
            mVertices[idx] = vertex;
            mUV[idx] = uv;
            mTriangles[mTriIdx++] = idx;
        }


        public void RenderTriangle(
            stVertexAtrribute a,
            stVertexAtrribute b,
            stVertexAtrribute c
                        )
        {
            RenderVertex(a.mVerticeIdx, a.mVertice, a.mUV);
            RenderVertex(b.mVerticeIdx, b.mVertice, b.mUV);
            RenderVertex(c.mVerticeIdx, c.mVertice, c.mUV);
        }


        public void Present()
        {
            if (mMesh != null)
            {
                mMesh.vertices = mVertices;
                mMesh.uv = mUV;
                mMesh.triangles = mTriangles;
                mMesh.normals = mNormals;
            }
        }


        public void Reset()
        {
            if (mVertices != null)
            {
                for (int i = 0; i < mVertices.Length; ++i)
                {
                    mVertices[i].x = mVertices[i].y = mVertices[i].z = 0;
                    if (mUV != null)
                    {
                        mUV[i].x = mUV[i].y = 0;
                    }
                    if (mNormals != null)
                    {
                        mNormals[i].x = mNormals[i].y = 0;
                    }
                }
            }

            mTriIdx = 0;
            if (mTriangles != null)
            {
                for (int i = 0; i < mTriangles.Length; ++i)
                {
                    mTriangles[i] = 0;
                }
            }

        }

    }


    #endregion

    #region 结点定义
    class CGeommPatch 
    {
        public float mDistance;
        public int mLOD;
        private int mPatchXIndex;
        public int PatchX
        {
            get
            {
                return mPatchXIndex;
            } 
        }

        private int mPatchZIndex; 
        public int PatchZ
        {
            get
            {
                return mPatchZIndex; 
            }
        }


        private GameObject mPatchGo;
        private Texture2D mColorTex;
        private Texture2D mDetailTex; 
        public Mesh mMesh;
        public Vector3[] mVertices;
        public Vector2[] mUV;
        public Vector3[] mNormals;
        public int[] mTriangles;


        private int mTriIdx;
        private int mPatchSize;

        public bool mbDrawLeft;
        public bool mbDrawTop;
        public bool mbDrawRight;
        public bool mbDrawBottom; 

        //Patch中点对应的顶点索引,在高度图中
        public int PatchCenterXIndex
        {
            get
            {
                return mPatchXIndex * mPatchSize + mPatchSize / 2;
            }
        }

        public int PatchCenterZIndex
        {
            get
            {
                return mPatchZIndex * mPatchSize + mPatchSize / 2;
            }
        }


        public int PatchCenterXInHeight
        {
            get
            {
                return mPatchXIndex * (mPatchSize - 1) + mPatchSize / 2;
            }
        }

        public int PatchCenterZInHeight
        {
            get
            {
                return mPatchZIndex * (mPatchSize - 1) + mPatchSize / 2;
            }
        }


        //Patch的中心点，在Pacth是属于什么位置
        public int CenterXInPatch
        {
            get
            {
                return mPatchSize / 2; 
            }
        }


        public int CenterZInPatch
        {
            get
            {
                return mPatchSize / 2; 
            }
        }



        public CGeommPatch(
            int patchX, 
            int patchZ,
            int patchSize ,  //奇数
            int initLOD,
            GameObject prefab,
            Texture2D colorTexture ,
            Texture2D detailTexture 
            )
        {
            //Patch的索引
            mPatchXIndex = patchX;
            mPatchZIndex = patchZ;


            mPatchSize = patchSize; 
            mTriIdx = 0;
            mLOD = initLOD;
            mDistance = 0.0f; 

            int vertexCnt = mPatchSize * mPatchSize;
            int trianglesCnt = (mPatchSize - 1) * (mPatchSize - 1) * 6;

            mColorTex = colorTexture;
            mDetailTex = detailTexture; 
            mVertices = new Vector3[vertexCnt];
            mNormals = new Vector3[vertexCnt];
            mUV = new Vector2[vertexCnt]; ;
            mTriangles = new int[trianglesCnt];
            mMesh = null;


            //生成纹理之类的
            mPatchGo = prefab; 
            if( mPatchGo != null )
            {
                //1、生成Mesh
                MeshFilter meshFilter = mPatchGo.GetComponent<MeshFilter>();
                if (null == meshFilter)
                {
                    Debug.LogError("Terrain without Comp [MeshFilter]");
                    return;
                }

                if (meshFilter.mesh == null)
                {
                    meshFilter.mesh = new Mesh();  
                }
                mMesh = meshFilter.mesh;

                //2、生成材质   
                MeshRenderer meshRender = mPatchGo.GetComponent<MeshRenderer>();
                if (meshRender != null)
                {
                    Shader terrainShader = Shader.Find("Terrain/QuadTree/TerrainRender");
                    if (terrainShader != null)
                    {
                        meshRender.material = new Material(terrainShader);
                        if (meshRender.material != null)
                        {
                            meshRender.material.SetTexture("_MainTex", mColorTex);
                            if (detailTexture != null)
                            {
                                meshRender.material.SetTexture("_DetailTex", mDetailTex);
                            }
                        }
                    }
                }
            }
        }   

        public void RenderVertex(
            int idx,
            Vector3 vertex,
            Vector3 uv
            )
        {
            mVertices[idx] = vertex;
            mUV[idx] = uv;
            mTriangles[mTriIdx++] = idx;
        }


        public void RenderTriangle(
            stVertexAtrribute a,
            stVertexAtrribute b,
            stVertexAtrribute c
                        )
        {
            RenderVertex(a.mVerticeIdx, a.mVertice, a.mUV);
            RenderVertex(b.mVerticeIdx, b.mVertice, b.mUV);
            RenderVertex(c.mVerticeIdx, c.mVertice, c.mUV);
        }


        public void RenderFan(
            stVertexAtrribute center,
            stVertexAtrribute bottomLeft, 
            stVertexAtrribute leftMid ,
            stVertexAtrribute topLeft,
            stVertexAtrribute topMid ,
            stVertexAtrribute topRight,
            stVertexAtrribute rightMid ,
            stVertexAtrribute bottomRight,
            stVertexAtrribute bottomMid,
            bool drawLeftMid ,
            bool drawTopMid,
            bool drawRightMid,
            bool drawBottomMid
            )
        {
            //左边的三角形扇
            if( drawLeftMid )
            {
                RenderTriangle(center, bottomLeft, leftMid);
                RenderTriangle(center, bottomLeft, topLeft); 
            }     
            else
            {
                RenderTriangle(center, bottomLeft, topLeft); 
            }

            //顶部的三角形扇
            if( drawTopMid )
            {
                RenderTriangle(center, topLeft, topMid);
                RenderTriangle(center, topLeft, topRight); 
            }
            else
            {
                RenderTriangle(center, topLeft, topRight); 
            }

            //右边的三角形扇
            if( drawRightMid )
            {
                RenderTriangle(center, topRight, rightMid);
                RenderTriangle(center, rightMid, bottomRight); 
            }
            else
            {
                RenderTriangle(center, topRight, bottomRight); 
            }

            //下方的三角形扇
            if( drawBottomMid )
            {
                RenderTriangle(center, bottomRight, bottomMid);
                RenderTriangle(center, bottomMid, bottomLeft); 
            }
            else
            {
                RenderTriangle(center, bottomRight, bottomLeft); 
            }
        }


        public void Present()
        {
            if (mMesh != null)
            {
                mMesh.vertices = mVertices;
                mMesh.uv = mUV;
                mMesh.triangles = mTriangles;
                mMesh.normals = mNormals;
            }
        }


        public void Reset()
        {
            if (mVertices != null)
            {
                for (int i = 0; i < mVertices.Length; ++i)
                {
                    mVertices[i].x = mVertices[i].y = mVertices[i].z = 0;
                    if (mUV != null)
                    {
                        mUV[i].x = mUV[i].y = 0;
                    }
                    if (mNormals != null)
                    {
                        mNormals[i].x = mNormals[i].y = 0;
                    }
                }
            }

            mTriIdx = 0;
            if (mTriangles != null)
            {
                for (int i = 0; i < mTriangles.Length; ++i)
                {
                    mTriangles[i] = 0;
                }
            }

        }// Reset 

        public void Render( stHeightData heightData , Vector3 vectorScale )
        {
            Profiler.BeginSample("CGeomipmappin.Render.Rebuild UV and Vertex");
            for (int z = 0; z < mPatchSize; ++z )
            {
                for(int x = 0; x < mPatchSize; ++x )
                {
                    //相对于Patch的偏移
                    int xOffsetFromPatchCentexX = x - CenterXInPatch;
                    int zOffsetFromPatchCentexZ = z - CenterZInPatch;

                    //顶点在Patch的位置
                    int inPatchIdx = z * mPatchSize + x;

                    //在高度图里面的位置
                    int indexX = PatchCenterXInHeight + xOffsetFromPatchCentexX;
                    int indexZ = PatchCenterZInHeight + zOffsetFromPatchCentexZ;
                    float height = heightData.GetRawHeightValue(indexX, indexZ) * vectorScale.y;
                    float xPos = indexX * vectorScale.x;
                    float zPos = indexZ * vectorScale.z; 

                    mVertices[inPatchIdx] = new Vector3(xPos , height , zPos);
                    mUV[inPatchIdx] = new Vector2((float)indexX / (float)heightData.mSize, (float)indexZ / (float)heightData.mSize);
                    mNormals[inPatchIdx] = Vector3.zero;
                }
            }

            Profiler.EndSample();


            Profiler.BeginSample("CGeomipmappin.Render.Rebuild Triangles");
            int nIdx = 0;
            for (int z = 0; z < mPatchSize - 1; ++z)
            {
                for (int x = 0; x <mPatchSize - 1; ++x)
                {
                    int bottomLeftIdx = z * mPatchSize + x;
                    int topLeftIdx = (z + 1) * mPatchSize + x;
                    int topRightIdx = topLeftIdx + 1;
                    int bottomRightIdx = bottomLeftIdx + 1;

                    mTriangles[nIdx++] = bottomLeftIdx;
                    mTriangles[nIdx++] = topLeftIdx;
                    mTriangles[nIdx++] = bottomRightIdx;
                    mTriangles[nIdx++] = topLeftIdx;
                    mTriangles[nIdx++] = topRightIdx;
                    mTriangles[nIdx++] = bottomRightIdx;
                }
            }

            Profiler.EndSample();
        }


    }

    #endregion

}
