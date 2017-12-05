using UnityEngine;
using Assets.Scripts.Common;
using Assets.Scripts.Utility;
using System.Collections.Generic; 

namespace Assets.Scripts.Geomipmapping
{


    class CGeomipmappingTerrain
    {

        #region   核心逻辑

        private int GetGlobalIndex(int x, int z)
        {
            return z * mHeightData.mSize + x;
        }


        #endregion



        #region  将模型渲染上去




        private Vector3 GetScaleVector3(float x, float z, Vector3 vectorScale)
        {
            return new Vector3(
                x * vectorScale.x,
                mHeightData.GetRawHeightValue((int)x, (int)z) * vectorScale.y,
                z * vectorScale.z
                );
        }


        private stVertexAtrribute GenerateVertex(
            int vertexIdx,
            float fX,
            float fZ,
            float uvX, float uvZ, Vector3 vectorScale)
        {
            return new stVertexAtrribute(
                 vertexIdx,
                 GetScaleVector3(fX, fZ, vectorScale),
                 new Vector2(uvX, uvZ)
                );
        }


       



        public void Render(Vector3 vertexScale)
        {
            for (int z = 0; z < mNumPatchesPerSize; ++z)
            {
                for (int x = 0; x < mNumPatchesPerSize; ++x)
                {
                    CGeommPatch patch = GetPatch(x, z);
                    if (null == patch)
                    {
                        continue;
                    }

                    patch.Reset(); 
                    patch.Render(mHeightData, vertexScale);
                    patch.Present(); 
                }
            }    
        }



        #endregion



        #region 地形纹理操作

        private List<CTerrainTile> mTerrainTiles = new List<CTerrainTile>();
        private Texture2D mTerrainTexture;
        public Texture2D TerrainTexture
        {
            get
            {
                return mTerrainTexture;
            }

        }



        public void GenerateTextureMap(uint uiSize, ushort maxHeight, ushort minHeight)
        {
            if (mTerrainTiles.Count <= 0)
            {
                return;
            }

            mTerrainTexture = null;
            int tHeightStride = maxHeight / mTerrainTiles.Count;

            float[] fBend = new float[mTerrainTiles.Count];

            //注意，这里的区域是互相重叠的
            int lastHeight = -1;
            for (int i = 0; i < mTerrainTiles.Count; ++i)
            {
                CTerrainTile terrainTile = mTerrainTiles[i];
                //lastHeight += 1;
                terrainTile.lowHeight = lastHeight + 1;
                lastHeight += tHeightStride;

                terrainTile.optimalHeight = lastHeight;
                terrainTile.highHeight = (lastHeight - terrainTile.lowHeight) + lastHeight;
            }

            for (int i = 0; i < mTerrainTiles.Count; ++i)
            {
                CTerrainTile terrainTile = mTerrainTiles[i];
                string log = string.Format("Tile Type:{0}|lowHeight:{1}|optimalHeight:{2}|highHeight:{3}",
                    terrainTile.TileType.ToString(),
                    terrainTile.lowHeight,
                    terrainTile.optimalHeight,
                    terrainTile.highHeight
                    );
                Debug.Log(log);
            }



            mTerrainTexture = new Texture2D((int)uiSize, (int)uiSize, TextureFormat.RGBA32, false);

            CUtility.SetTextureReadble(mTerrainTexture, true);

            float fMapRatio = (float)mHeightData.mSize / uiSize;

            for (int z = 0; z < uiSize; ++z)
            {
                for (int x = 0; x < uiSize; ++x)
                {

                    Color totalColor = new Color();

                    for (int i = 0; i < mTerrainTiles.Count; ++i)
                    {
                        CTerrainTile tile = mTerrainTiles[i];
                        if (tile.mTileTexture == null)
                        {
                            continue;
                        }

                        int uiTexX = x;
                        int uiTexZ = z;

                        //CUtility.SetTextureReadble(tile.mTileTexture, true);

                        GetTexCoords(tile.mTileTexture, ref uiTexX, ref uiTexZ);



                        Color color = tile.mTileTexture.GetPixel(uiTexX, uiTexZ);
                        fBend[i] = RegionPercent(tile.TileType, Limit(InterpolateHeight(x, z, fMapRatio), maxHeight, minHeight));

                        totalColor.r = Mathf.Min(color.r * fBend[i] + totalColor.r, 1.0f);
                        totalColor.g = Mathf.Min(color.g * fBend[i] + totalColor.g, 1.0f);
                        totalColor.b = Mathf.Min(color.b * fBend[i] + totalColor.b, 1.0f);
                        totalColor.a = 1.0f;

                        //CUtility.SetTextureReadble(tile.mTileTexture, false);
                    }// 

                    //输出到纹理上
                    if (totalColor.r == 0.0f
                        && totalColor.g == 0.0f
                        && totalColor.b == 0.0f)
                    {
                        ushort xHeight = (ushort)(x * fMapRatio);
                        ushort zHeight = (ushort)(z * fMapRatio);
                        Debug.Log(string.Format("Color is Black | uiX:{0}|uiZ:{1}|hX:{2}|hZ:{3}|h:{4}", x, z, xHeight, zHeight, GetTrueHeightAtPoint(xHeight, zHeight)));
                    }

                    mTerrainTexture.SetPixel(x, z, totalColor);
                }
            }

            //OpenGL纹理的操作
            mTerrainTexture.Apply();
            CUtility.SetTextureReadble(mTerrainTexture, false);

            //string filePath = string.Format("{0}/{1}", Application.dataPath, "Runtime_TerrainTexture.png"); 
            //File.WriteAllBytes(filePath,mTerrainTexture.EncodeToPNG());
            //AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.ForceUpdate);
            //AssetDatabase.SaveAssets();
        } // 


        public CTerrainTile GetTile(enTileTypes tileType)
        {
            return mTerrainTiles.Count > 0 ? mTerrainTiles.Find(curTile => curTile.TileType == tileType) : null;
        }


        float RegionPercent(enTileTypes tileType, ushort usHeight)
        {
            CTerrainTile tile = GetTile(tileType);
            if (tile == null)
            {
                Debug.LogError(string.Format("No tileType : Type:{0}|Height:{1}", tileType.ToString(), usHeight));
                return 0.0f;
            }

            CTerrainTile lowestTile = GetTile(enTileTypes.lowest_tile);
            CTerrainTile lowTile = GetTile(enTileTypes.low_tile);
            CTerrainTile highTile = GetTile(enTileTypes.high_tile);
            CTerrainTile highestTile = GetTile(enTileTypes.highest_tile);

            //如果最低的块已经加载了，且落在它的low Height的块里面
            if (lowestTile != null)
            {
                if (tileType == enTileTypes.lowest_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }

            else if (lowTile != null)
            {
                if (tileType == enTileTypes.low_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }
            else if (highTile != null)
            {
                if (tileType == enTileTypes.high_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }
            else if (highestTile != null)
            {
                if (tileType == enTileTypes.highest_tile
                    && IsHeightAllLocateInTile(tileType, usHeight))
                {
                    return 1.0f;
                }
            }

            //以[,)左闭右开吧
            if (usHeight < tile.lowHeight || usHeight > tile.highHeight)
            {
                return 0.0f;
            }


            if (usHeight < tile.optimalHeight)
            {
                float fTemp1 = usHeight - tile.lowHeight;
                float fTemp2 = tile.optimalHeight - tile.lowHeight;

                //这段会产生小斑点，因为有些值可能会比较特殊
                //if (fTemp1 == 0.0f)
                //{
                //    Debug.LogError(string.Format("Lower than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|lowHeight:{3}|optimalHeight:{4}", tileType.ToString(), usHeight, fTemp1, tile.lowHeight, tile.optimalHeight));
                //    return 1.0f;
                //}
                return fTemp1 / fTemp2;
            }
            else if (usHeight == tile.optimalHeight)
            {
                return 1.0f;
            }
            else if (usHeight > tile.optimalHeight)
            {
                float fTemp1 = tile.highHeight - tile.optimalHeight;

                //这段会产生小斑点，因为有些值可能会比较特殊
                //if (((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1) == 0.0f)
                //{
                //    Debug.LogError(string.Format("Higher than Optimal Height: Type:{0}|Height:{1}|fTemp1:{2}|optimalHeight:{3}", tileType.ToString(), usHeight, fTemp1, tile.optimalHeight));
                //    return 1.0f;
                //}
                return ((fTemp1 - (usHeight - tile.optimalHeight)) / fTemp1);
            }

            Debug.LogError(string.Format("Unknow: Type:{0}|Height:{1}", tileType.ToString(), usHeight));
            return 0.0f;
        }


        private ushort GetTrueHeightAtPoint(int x, int z)
        {
            return mHeightData.GetRawHeightValue(x, z);
        }

        //两个高度点之间的插值，这里的很有意思的
        private ushort InterpolateHeight(int x, int z, float fHeight2TexRatio)
        {
            float fScaledX = x * fHeight2TexRatio;
            float fScaledZ = z * fHeight2TexRatio;

            ushort usHighX = 0;
            ushort usHighZ = 0;

            //X的A点
            ushort usLow = GetTrueHeightAtPoint((int)fScaledX, (int)fScaledZ);

            if ((fScaledX + 1) > mHeightData.mSize)
            {
                return usLow;
            }
            else
            {
                //X的B点
                usHighX = GetTrueHeightAtPoint((int)fScaledX + 1, (int)fScaledZ);
            }

            //X的A、B两点之间插值
            float fInterpolation = (fScaledX - (int)fScaledX);
            float usX = (usHighX - usLow) * fInterpolation + usLow;   //插值出真正的高度值 


            //Z轴同理
            if ((fScaledZ + 1) > mHeightData.mSize)
            {
                return usLow;
            }
            else
            {
                //X的B点
                usHighZ = GetTrueHeightAtPoint((int)fScaledX, (int)fScaledZ + 1);
            }

            fInterpolation = (fScaledZ - (int)fScaledZ);
            float usZ = (usHighZ - usLow) * fInterpolation + usLow;   //插值出真正的高度值

            return ((ushort)((usX + usZ) / 2));
        }

        private ushort Limit(ushort usValue, ushort maxHeight, ushort minHeight)
        {
            if (usValue > maxHeight)
            {
                return maxHeight;
            }
            else if (usValue < minHeight)
            {
                return minHeight;
            }
            return usValue;
        }


        private bool IsHeightAllLocateInTile(enTileTypes tileType, ushort usHeight)
        {
            bool bRet = false;
            CTerrainTile tile = GetTile(tileType);
            if (tile != null
                && usHeight <= tile.optimalHeight)
            {
                bRet = true;
            }
            return bRet;
        }


        //因为要渲染出来的一张地形纹理，可能会比tile的宽高都要大，所以要tile其实是平铺布满地形纹理的
        public void GetTexCoords(Texture2D texture, ref int x, ref int y)
        {
            int uiWidth = texture.width;
            int uiHeight = texture.height;

            int tRepeatX = -1;
            int tRepeatY = -1;
            int i = 0;

            while (tRepeatX == -1)
            {
                i++;
                if (x < (uiWidth * i))
                {
                    tRepeatX = i - 1;
                }
            }


            i = 0;
            while (tRepeatY == -1)
            {
                ++i;
                if (y < (uiHeight * i))
                {
                    tRepeatY = i - 1;
                }
            }


            x = x - (uiWidth * tRepeatX);
            y = y - (uiHeight * tRepeatY);
        }




        public void AddTile(enTileTypes tileType, Texture2D tileTexture)
        {
            if (tileTexture != null)
            {
                if (mTerrainTiles.Exists(curTile => curTile.TileType == tileType))
                {
                    CTerrainTile oldTile = mTerrainTiles.Find(curTile => curTile.TileType == tileType);
                    if (oldTile != null)
                    {
                        oldTile = new CTerrainTile(tileType, tileTexture);
                    }
                }
                else
                {
                    mTerrainTiles.Add(new CTerrainTile(tileType, tileTexture));
                }
            }
        }


        #endregion

        #region 高度图数据操作

        private stHeightData mHeightData;

        public void UnloadHeightMap()
        {
            mHeightData.Release();

            Debug.Log("Height Map is Unload!");
        }






        /// <summary>
        /// This fuction came from book 《Focus On 3D Terrain Programming》 ,thanks Trent Polack a lot
        /// </summary>
        /// <param name="size"></param>
        /// <param name="iter"></param>
        /// <param name="minHeightValue"></param>
        /// <param name="maxHeightValue"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public bool MakeTerrainFault(int size, int iter, ushort minHeightValue, ushort maxHeightValue, float fFilter)
        {
            if (mHeightData.IsValid())
            {
                UnloadHeightMap();
            }

            mHeightData.Allocate(size);

            float[,] fTempHeightData = new float[size, size];

            for (int iCurIter = 0; iCurIter < iter; ++iCurIter)
            {
                //高度递减
                int tHeight = maxHeightValue - ((maxHeightValue - minHeightValue) * iCurIter) / iter;
                //int tHeight = Random.Range(minHeightValue , maxHeightValue);  //temp 

                int tRandomX1 = Random.Range(0, size);
                int tRandomZ1 = Random.Range(0, size);

                int tRandomX2 = 0;
                int tRandomZ2 = 0;
                do
                {
                    tRandomX2 = Random.Range(0, size);
                    tRandomZ2 = Random.Range(0, size);
                } while (tRandomX2 == tRandomX1 && tRandomZ2 == tRandomZ1);


                //两个方向的矢量
                int tDirX1 = tRandomX2 - tRandomX1;
                int tDirZ1 = tRandomZ2 - tRandomZ1;

                //遍历每个顶点，看看分布在分割线的哪一边
                for (int z = 0; z < size; ++z)
                {
                    for (int x = 0; x < size; ++x)
                    {
                        int tDirX2 = x - tRandomX1;
                        int tDirZ2 = z - tRandomZ1;

                        if ((tDirX2 * tDirZ1 - tDirX1 * tDirZ2) > 0)
                        {
                            fTempHeightData[x, z] += tHeight; //!!!!!自加符号有问题！！！！
                        }
                    }
                }

                FilterHeightField(ref fTempHeightData, size, fFilter);

            }

            NormalizeTerrain(ref fTempHeightData, size, maxHeightValue);

            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    SetHeightAtPoint((ushort)fTempHeightData[x, z], x, z);
                }
            }

            return true;
        }


        void SetHeightAtPoint(ushort usHeight, int x, int z)
        {
            mHeightData.SetHeightValue(usHeight, x, z);
        }


        void NormalizeTerrain(ref float[,] fHeightData, int size, ushort maxHeight)
        {
            float fMin = fHeightData[0, 0];
            float fMax = fHeightData[0, 0];

            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    if (fHeightData[x, z] > fMax)
                    {
                        fMax = fHeightData[x, z];
                    }

                    if (fHeightData[x, z] < fMin)
                    {
                        fMin = fHeightData[x, z];
                    }

                }
            }

            Debug.Log(string.Format("Before Normailzed MaxHeight:{0}|MinHeight:{1}", fMax, fMin));

            if (fMax <= fMin)
            {
                return;
            }

            float fHeight = fMax - fMin;
            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    fHeightData[x, z] = ((fHeightData[x, z] - fMin) / fHeight) * maxHeight;
                }
            }


            ///////////////打LOG用
            fMax = fHeightData[0, 0];
            fMin = fHeightData[0, 0];
            for (int z = 0; z < size; ++z)
            {
                for (int x = 0; x < size; ++x)
                {
                    if (fHeightData[x, z] > fMax)
                    {
                        fMax = fHeightData[x, z];
                    }

                    if (fHeightData[x, z] < fMin)
                    {
                        fMin = fHeightData[x, z];
                    }
                }
            }

            Debug.Log(string.Format("After Normailzed MaxHeight:{0}|MinHeight:{1}", fMax, fMin));

            ///////////////////////////
        }

        void FilterHeightField(ref float[,] fHeightData, int size, float fFilter)
        {
            //四向模糊

            //从左往右的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    i, 0,  //初始的x,y
                    0, 1,       //数组步进值
                    size,      //数组个数
                    fFilter);
            }

            //从右往左的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    i, size - 1,
                    0, -1,
                    size,
                    fFilter);
            }


            //从上到下的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    0, i,
                    1, 0,
                    size,
                    fFilter);
            }


            //从下到上的模糊
            for (int i = 0; i < size; ++i)
            {
                FilterHeightBand(ref fHeightData,
                    size - 1, i,
                    -1, 0,
                    size,
                    fFilter);
            }
        }



        void FilterHeightBand(
            ref float[,] fBandData,
            int beginX,
            int beginY,
            int strideX,
            int strideY,
            int count,
            float fFilter)
        {
            //Debug.Log(string.Format("BeginX:{0} | BeginY:{1} | StrideX:{2} | StrideY:{3}",beginX,beginY,strideX,strideY)); 

            //float beginValue = fBandData[beginX, beginY];
            float curValue = fBandData[beginX, beginY];
            int jx = strideX;
            int jy = strideY;

            //float delta = fFilter / (count - 1);
            for (int i = 0; i < count - 1; ++i)
            {
                int nextX = beginX + jx;
                int nextY = beginY + jy;

                fBandData[nextX, nextY] = fFilter * curValue + (1 - fFilter) * fBandData[nextX, nextY];
                curValue = fBandData[nextX, nextY];

                //float tFilter = fFilter - delta * ((jx - beginX + jy - beginY) * 0.5f);
                //fBandData[nextX, nextY] = tFilter * beginValue;

                jx += strideX;
                jy += strideY;
            }
        }


        #endregion

        #region Patch相关操作
        private int mMaxLOD;
        private int mPatchSize;
        private List<CGeommPatch> mGeommPatchs = new List<CGeommPatch>(); 
        private int mNumPatchesPerSize
        {
            get
            {
                return mHeightData.mSize / mPatchSize; 
            }
        }


        public void CombineMesh(  GameObject terrainGo ,Texture2D  detailTexture )
        {
            if( terrainGo != null )
            {
                MeshFilter rootMeshFilter = terrainGo.GetComponent<MeshFilter>(); 
                if( rootMeshFilter != null )
                {
                    Mesh rootMesh = null;
                    if (rootMeshFilter.mesh == null)
                    {
                        rootMeshFilter.mesh = new Mesh();
                    }//if mesh 

                    //2、生成材质   
                    MeshRenderer meshRender = terrainGo.GetComponent<MeshRenderer>();
                    if (meshRender != null)
                    {
                        Shader terrainShader = Shader.Find("Terrain/Geomipmapping/TerrainRender");
                        if (terrainShader != null)
                        {
                            meshRender.material = new Material(terrainShader);
                            meshRender.material.SetTexture("_MainTex", mTerrainTexture);
                            if (detailTexture != null)
                            {
                                meshRender.material.SetTexture("_DetailTex", detailTexture);
                            }
                        }
                    }  //mesh Render


                    CombineInstance[] need2CombineMeshs = new CombineInstance[mGeommPatchs.Count]; 
                    rootMesh = rootMeshFilter.mesh; 
                    if( rootMesh != null )
                    {
                        for (int i = 0; i < mGeommPatchs.Count; ++i)
                        {
                            MeshFilter meshFilter = mGeommPatchs[i].PatchMeshFilter; 
                            Mesh mesh = mGeommPatchs[i].PatchMesh;
                            if ( mesh && meshFilter )
                            {
                                need2CombineMeshs[i].mesh = mesh;
                                need2CombineMeshs[i].transform = meshFilter.transform.localToWorldMatrix;
                                meshFilter.gameObject.SetActive(false); 
                            }
                        }
                    }

                    rootMesh.CombineMeshes(need2CombineMeshs); 
                }
            }
        }



        /// <summary>
        /// 每条边有多少个顶点
        /// </summary>
        /// <param name="oneSideVertexPerPatch"></param>
        public void ConfigGeommaping(
            int vertexPerPatch,
            GameObject patchPrefab,
            GameObject patchParent,
            Texture2D colorTexture,
            Texture2D detailTexture )
        {
            if( vertexPerPatch > 0 
                && mHeightData.IsValid() )
            {
                mPatchSize = vertexPerPatch;

                int tDivisor = vertexPerPatch - 1;
                int tLOD = 0; 
                while( tDivisor > 2  )
                {
                    tDivisor = tDivisor >> 1;
                    tLOD++; 
                }

                mMaxLOD = tLOD; 

                  
                for(int z = 0; z < mNumPatchesPerSize; z++ )
                {
                    for(int x = 0; x < mNumPatchesPerSize; x++)
                    {
                        GameObject patchGo = GameObject.Instantiate(patchPrefab, Vector3.zero, Quaternion.identity) as GameObject;
                        if( patchGo != null )
                        {
                            patchGo.name = string.Format("{0}_{1:D2}{2:D2}", patchGo.name, x, z); 
                            patchGo.transform.SetParent(patchParent.transform);
                        }

                        CGeommPatch patch = new CGeommPatch(
                            x,
                            z,
                            mPatchSize,
                            mMaxLOD,
                            patchGo,
                            colorTexture,
                            detailTexture
                            );

                        mGeommPatchs.Add(patch);
                    }
                }
                 
            }
        }

        public void CLOD_Render(Vector3 vectorScale)
        {
            for (int z = 0; z < mNumPatchesPerSize; ++z)
            {
                for (int x = 0; x < mNumPatchesPerSize; ++x)
                {
                    CGeommPatch patch = GetPatch(x, z);
                    if (null == patch)
                    {
                        continue;
                    }

                    int curPatchLOD = patch.mLOD;
                    CGeommPatch leftNeighborPatch = GetPatch(x - 1, z);
                    CGeommPatch topNeighborPatch = GetPatch(x, z + 1);
                    CGeommPatch rightNeighborPatch = GetPatch(x + 1, z);
                    CGeommPatch bottomNeighborPatch = GetPatch(x, z - 1);

                    //需要画左边中间的点
                    patch.mbDrawLeft = CanDrawMidVertex(curPatchLOD, leftNeighborPatch);
                    patch.mbDrawTop = CanDrawMidVertex(curPatchLOD, topNeighborPatch);
                    patch.mbDrawRight = CanDrawMidVertex(curPatchLOD, rightNeighborPatch);
                    patch.mbDrawBottom = CanDrawMidVertex(curPatchLOD, bottomNeighborPatch);

                    patch.Reset();

                    if( patch.mbIsVisible )
                    {
                        Profiler.BeginSample("Geomipmapping.RenderPatch");
                        RenderPatch(patch, vectorScale);
                        Profiler.EndSample();
                    }

                    Profiler.BeginSample("Geomipmapping.Present");
                    patch.Present();
                    Profiler.EndSample();
                }
            }
        }
       

        public void RenderPatch(CGeommPatch patch , Vector3 vectorScale )
        {
            if( null == patch )
            {
                return; 
            }

            int iSize = mPatchSize;
  
            int iDivisor = mPatchSize - 1;
            int tLOD = patch.mLOD;

            while (tLOD >= 0)
            {
                iDivisor = iDivisor >> 1;
                tLOD--; 
            }

            iSize /= iDivisor;
            int iHalfSize = iSize / 2;

            //Patch是从左往右,从下到上，而不是先中心点开始绘制的
            for (int inPatchZ = iHalfSize; (inPatchZ+iHalfSize) < mPatchSize + 1 ; inPatchZ+= iSize )
            {
                for (int inPatchX = iHalfSize; (inPatchX + iHalfSize) < mPatchSize + 1 ; inPatchX+=iSize )
                {
                    bool bDrawLeft = false;
                    bool bDrawTop = false;
                    bool bDrawRight = false;
                    bool bDrawBottom = false; 

                    //最左边的Fan
                    if( inPatchX == iHalfSize )
                    {
                        bDrawLeft = patch.mbDrawLeft;                           
                    }
                    else
                    {
                        bDrawLeft = true;   //如果是内部的Fan，即中点必须画 
                    }

                    if (inPatchZ == iHalfSize)
                    {
                        bDrawBottom = patch.mbDrawBottom;
                    }
                    else
                    {
                        bDrawBottom = true;   //如果是内部的Fan，即中点必须画 
                    }


                    if (inPatchX >= (mPatchSize - 1 - iHalfSize))  //左边括号的那坨东西是代表最后一个顶点
                    {
                        bDrawRight = patch.mbDrawRight;
                    }
                    else
                    {
                        bDrawRight = true;   //如果是内部的Fan，即中点必须画 
                    }


                    if (inPatchZ >= ( mPatchSize - 1 -  iHalfSize))
                    {
                        bDrawTop = patch.mbDrawTop;
                    }
                    else
                    {
                        bDrawTop = true;   //如果是内部的Fan，即中点必须画 
                    }

                    Profiler.BeginSample("Geomipmapping.RenderFan"); 
                    RenderFan( inPatchX, inPatchZ, iSize, bDrawLeft, bDrawTop, bDrawRight, bDrawBottom, patch, vectorScale);
                    Profiler.EndSample(); 
                }
            }
        }

        private void RenderFan( int inPatchX,int inPatchZ,int fanSize, bool drawLeft,bool drawTop,bool drawRight,bool drawBottom,
            CGeommPatch patch, Vector3 vectorScale)
        {
            if( null == patch )
            {
                return; 
            }

            float fHalfSize = fanSize / 2.0f;
            int iHalfSize = (int)fHalfSize; 

            //在Patch里面的顶点位置
            int xCenterInPatchVertexs = (int)inPatchX;
            int zCenterInPatchVertexs = (int)inPatchZ;
            int xLeftInPatchVertexs = xCenterInPatchVertexs - iHalfSize;
            int zTopInPatchVertexs = zCenterInPatchVertexs + iHalfSize;
            int xRightInPatchVertexs = xCenterInPatchVertexs + iHalfSize;
            int zBottomInPatchVertexs = zCenterInPatchVertexs - iHalfSize; 

            //相对于Patch中心点的偏移
            int xOffsetFromPatchCentexX = xCenterInPatchVertexs - patch.CenterXInPatch;
            int zOffsetFromPatchCentexZ = zCenterInPatchVertexs - patch.CenterZInPatch; 

          
            //在高度图里面的位置
            float fanCenterRawXInHeight = patch.PatchCenterXInHeight + xOffsetFromPatchCentexX;  
            float fanCenterRawZInHeight = patch.PatchCenterZInHeight + zOffsetFromPatchCentexZ ;
            float fanLeftRawXInHeight = fanCenterRawXInHeight - iHalfSize;
            float fanRightRawXInHeight = fanCenterRawXInHeight + iHalfSize;
            float fanTopRawZInHeight = fanCenterRawZInHeight + iHalfSize;
            float fanBottomRawZInHeight = fanCenterRawZInHeight - iHalfSize;

            float fTexLeft = ((float)Mathf.Abs(fanCenterRawXInHeight - fHalfSize) / mHeightData.mSize) ;
            float fTexBottom = ((float)Mathf.Abs(fanCenterRawZInHeight - fHalfSize) / mHeightData.mSize)  ;
            float fTexRight = ((float)Mathf.Abs(fanCenterRawXInHeight + fHalfSize) / mHeightData.mSize)  ;
            float fTexTop = ((float)Mathf.Abs(fanCenterRawZInHeight + fHalfSize) / mHeightData.mSize)  ;

            float fMidX = ((fTexLeft + fTexRight) / 2);
            float fMidZ = ((fTexBottom + fTexTop) / 2);


            stVertexAtrribute centerVertex = GenerateVertex( GetPatchVertexIndex(xCenterInPatchVertexs,zCenterInPatchVertexs)  , fanCenterRawXInHeight, fanCenterRawZInHeight, fMidX, fMidZ, vectorScale);
            stVertexAtrribute bottomLeftVertex = GenerateVertex(GetPatchVertexIndex(xLeftInPatchVertexs, zBottomInPatchVertexs), fanLeftRawXInHeight, fanBottomRawZInHeight, fTexLeft, fTexBottom, vectorScale);
            stVertexAtrribute leftMidVertex = GenerateVertex(GetPatchVertexIndex(xLeftInPatchVertexs, zCenterInPatchVertexs), fanLeftRawXInHeight, fanCenterRawZInHeight, fTexLeft, fMidZ, vectorScale);
            stVertexAtrribute topLeftVertex = GenerateVertex(GetPatchVertexIndex(xLeftInPatchVertexs, zTopInPatchVertexs), fanLeftRawXInHeight, fanTopRawZInHeight, fTexLeft, fTexTop, vectorScale);
            stVertexAtrribute topMidVertex = GenerateVertex(GetPatchVertexIndex(xCenterInPatchVertexs, zTopInPatchVertexs), fanCenterRawXInHeight, fanTopRawZInHeight, fMidX, fTexTop, vectorScale);
            stVertexAtrribute topRightVertex = GenerateVertex(GetPatchVertexIndex(xRightInPatchVertexs, zTopInPatchVertexs), fanRightRawXInHeight, fanTopRawZInHeight, fTexRight, fTexTop, vectorScale);
            stVertexAtrribute rightMidVertex = GenerateVertex(GetPatchVertexIndex(xRightInPatchVertexs, zCenterInPatchVertexs), fanRightRawXInHeight, fanCenterRawZInHeight, fTexRight, fMidZ, vectorScale);
            stVertexAtrribute bottomRightVertex = GenerateVertex(GetPatchVertexIndex(xRightInPatchVertexs, zBottomInPatchVertexs), fanRightRawXInHeight, fanBottomRawZInHeight, fTexRight, fTexBottom, vectorScale);
            stVertexAtrribute bottomMidVertex = GenerateVertex(GetPatchVertexIndex(xCenterInPatchVertexs, zBottomInPatchVertexs), fanCenterRawXInHeight, fanBottomRawZInHeight, fMidX, fTexBottom, vectorScale);


            patch.RenderFan(
                centerVertex,
                bottomLeftVertex,
                leftMidVertex,
                topLeftVertex,
                topMidVertex,
                topRightVertex,
                rightMidVertex,
                bottomRightVertex,
                bottomMidVertex,
                drawLeft,
                drawTop,
                drawRight,
                drawBottom
                );
        }


        private bool CanDrawMidVertex( int lod , CGeommPatch neighborPatch  )
        {
            bool ret = false;
            if( null == neighborPatch || neighborPatch.mLOD <= lod  || !neighborPatch.mbIsVisible )
            {
                ret = true; 
            }
            return ret; 
        }


        public void UpdatePatch( Camera viewCamera, Vector3 vectorScale , List<float> lodLevels )
        {
            if (null == viewCamera)
            {
                Debug.LogError("[UpdatePatch]View Camera is Null!");
                return ;
            }

            if( null == lodLevels || 0 == lodLevels.Count)
            {
                Debug.LogError("[UpdatePatch]LOD Levels is Null!");
                return;
            }

            Profiler.BeginSample("Geomipmapping.CalculateFrustumPlanes");
            Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(viewCamera);
            Profiler.EndSample();


            for (int z = 0; z < mNumPatchesPerSize; z++)
            {
                for(int x = 0; x < mNumPatchesPerSize;x++)
                {
                    CGeommPatch patch = GetPatch(x, z);
                    if( null == patch )
                    {
                        continue; 
                    }

                    bool patchIsVisible = true; 
                    if( frustumPlanes != null  )
                    {
                        Profiler.BeginSample("Geomipmapping.TestPlanesAABB");
                        patchIsVisible =  GeometryUtility.TestPlanesAABB(frustumPlanes, patch.PatchBounds);
                        Profiler.EndSample(); 
                    }

                    patch.mbIsVisible = patchIsVisible; 
                    if( patchIsVisible )
                    {
                        float patchCenterX = patch.PatchCenterXInHeight * vectorScale.x;
                        float patchCenterZ = patch.PatchCenterZInHeight * vectorScale.z;
                        float patchCenterY = mHeightData.GetRawHeightValue(patch.PatchCenterXInHeight, patch.PatchCenterZInHeight) * vectorScale.y;

                        patch.mDistance = Mathf.Sqrt(
                               Mathf.Pow(viewCamera.transform.position.x - patchCenterX, 2) +
                               Mathf.Pow(viewCamera.transform.position.y - patchCenterY, 2) +
                               Mathf.Pow(viewCamera.transform.position.z - patchCenterZ, 2)
                                );


                        patch.mLOD = mMaxLOD;
                        for (int i = 0; i < lodLevels.Count; ++i)
                        {
                            float lodDistance = lodLevels[i];
                            if (patch.mDistance < lodDistance)
                            {
                                patch.mLOD = i;
                                break;
                            }
                        }
                    }
                }
            }
        }


        private CGeommPatch GetPatch( int x, int z )
        {
            //不合法的输入直接排队
            if (x < 0 || x >= mNumPatchesPerSize || z < 0 || z >= mNumPatchesPerSize)
            {
                return null;
            }

            int idx = GetPatchIndex(x, z);
            return (idx >= 0 && idx < mGeommPatchs.Count) ? mGeommPatchs[idx] : null;
        }


        private int GetPatchIndex(int x, int z )
        {
            return z * mNumPatchesPerSize + x;
        }

        private int GetPatchVertexIndex(int x, int z )
        {
            return z * mPatchSize + x;
        }

        #endregion

    }
}
