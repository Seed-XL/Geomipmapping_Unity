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
    struct stGeommPatchData 
    {
        public float mDistance;
        public int mLOD;
        private int mPatchXIndex;
        private int mPatchZIndex;

        private GameObject mPatchGo; 
        public Mesh mMesh;
        public Vector3[] mVertices;
        public Vector2[] mUV;
        public Vector3[] mNormals;
        public int[] mTriangles;


        private int mTriIdx;
        private int mPatchsPerSide;
        private int mPatchSize;
        private int mHeightMapSize;

        public stGeommPatchData(
            int x, 
            int z,
            int patchSize ,
            int patchsPerSide,
            int heightMapSize,
            int initLOD,
            GameObject prefab
            )
        {
            mPatchXIndex = x;
            mPatchZIndex = z;
            mPatchSize = patchSize; 
            mPatchsPerSide = patchsPerSide;
            mHeightMapSize = heightMapSize; 
            mTriIdx = 0;
            mLOD = initLOD;
            mDistance = 0.0f; 

            Vector3 patchPos = new Vector3(x, 0, z);

            mPatchGo = GameObject.Instantiate(prefab, patchPos, Quaternion.identity) as GameObject; 
            if( mPatchGo != null )
            {
                //1、生成Mesh
                //2、生成材质   
                //3、生成纹理  
            }


            mMesh = new Mesh();
            mVertices = null;
            mNormals = null;
            mUV = null; 
            mTriangles = null; 

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

}
