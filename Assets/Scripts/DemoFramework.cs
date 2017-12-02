﻿using UnityEngine;
using Assets.Scripts.Geomipmapping;
using Assets.Scripts.Common;
using System.Collections.Generic; 


public class DemoFramework : MonoBehaviour {

    #region 输入操作
    public float movementSpeed = 1f;
    public float mouseSensitive = 1f;

    public float mouseScrollSensitive = 1.0f;

    private RenderInWireframe mWireFrameCtrl;

    #endregion


    public bool renderGeoMappingCLOD = false;

    //摄像机对象
    public GameObject cameraGo;
    public Camera renderCamera;
    //地形对象
    public GameObject terrainGo;

    public GameObject patchPrefab; 

    //顶点间的距离
    public Vector3 vertexScale;

    //高度图的边长,也就是结点的个数
    public int heightSize;
    public int vertexsPerPatch; 

    //是否从高度图读取高度信息
    //True从文件读取
    //False动态生成
    public bool isLoadHeightDataFromFile;
    public string heightFileName;

    public bool isGenerateHeightDataRuntime;
    public int iterations;
    [Range(0, 255)]
    public int minHeightValue;
    [Range(0, 65536)]
    public int maxHeightValue;
    [Range(0, 0.9f)]
    public float filter;


    #region  LOD 
    [SerializeField]
    public List<float> lodLevels = new List<float>(); 

    public void ConfigLODHierarchys(int patchSize , float stepValue = 500.0f)
    {
        int tDivisor = patchSize - 1;
        int tLOD = 0;
        while (tDivisor > 2)
        {
            tDivisor = tDivisor >> 1;
            tLOD++;
        }

        lodLevels.Clear(); 
        float baseValue = 0; 
        for(int i = 0; i < tLOD - 1 ; ++i)
        {
            baseValue += stepValue;
            float lodValue = baseValue ;
            lodLevels.Add(lodValue);      
        }
    }

    #endregion


    #region 地图Tile
    public Texture2D detailTexture;

    [Range(1, 2048)]
    public int terrainTextureSize = 256;
    public Texture2D[] tiles;

    #endregion



    private CGeomipmappingTerrain mGeoMappingTerrain;



    #region  顶点数据放里

    private stTerrainMeshData mMeshData; 

    #endregion


    //1、读取高度图，
    //2、设置顶点间距离，
    //3、读取纹理
    //4、设置光照阴影
    void Start()
    {
        InitMeshData();
        InitRenderMode(); 
    
        mGeoMappingTerrain = new CGeomipmappingTerrain();
        //制造高度图
        mGeoMappingTerrain.MakeTerrainFault(heightSize, iterations, (ushort)minHeightValue, (ushort)maxHeightValue, filter);
        mGeoMappingTerrain.ConfigGeommaping(vertexsPerPatch, patchPrefab); 

        //设置对应的纹理块
        AddTile(enTileTypes.lowest_tile);
        AddTile(enTileTypes.low_tile);
        AddTile(enTileTypes.high_tile);
        AddTile(enTileTypes.highest_tile);
        mGeoMappingTerrain.GenerateTextureMap((uint)terrainTextureSize, (ushort)maxHeightValue, (ushort)minHeightValue);
        ApplyTerrainTexture(mGeoMappingTerrain.TerrainTexture);

    }


    #region 地图块操作


    private void ApplyTerrainTexture(Texture2D texture)
    {
        if (terrainGo != null)
        {
            MeshRenderer meshRender = terrainGo.GetComponent<MeshRenderer>();
            if (meshRender != null)
            {
                Shader terrainShader = Shader.Find("Terrain/QuadTree/TerrainRender");
                if (terrainShader != null)
                {
                    meshRender.material = new Material(terrainShader);
                    if (meshRender.material != null)
                    {
                        meshRender.material.SetTexture("_MainTex", texture);
                        if (detailTexture != null)
                        {
                            meshRender.material.SetTexture("_DetailTex", detailTexture);
                        }
                    }
                }
            }
        }
    }


    void AddTile(enTileTypes tileType)
    {
        int tileIdx = (int)tileType;
        if (tileIdx < tiles.Length
            && tiles[tileIdx] != null)
        {
            mGeoMappingTerrain.AddTile((enTileTypes)tileIdx, tiles[tileIdx]);
        }
    }


    #endregion


    #region 输入操作

    public void DemoInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (cameraGo != null
            && renderCamera)
        {
            //鼠标操作

            // 滚轮实现镜头缩进和拉远
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                renderCamera.fieldOfView = renderCamera.fieldOfView - Input.GetAxis("Mouse ScrollWheel") * mouseScrollSensitive;
                renderCamera.fieldOfView = Mathf.Clamp(renderCamera.fieldOfView, renderCamera.nearClipPlane, renderCamera.farClipPlane);
            }
            //鼠标右键实现视角转动，类似第一人称视角
            if (Input.GetMouseButton(0))
            {
                float rotationX = Input.GetAxis("Mouse X") * mouseSensitive;
                float rotationY = Input.GetAxis("Mouse Y") * mouseSensitive;
                cameraGo.transform.Rotate(-rotationY, rotationX, 0);
            }


            //键盘操作
            if (Input.GetKey(KeyCode.UpArrow))
            {
                cameraGo.transform.Translate(transform.forward * movementSpeed, Space.Self);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                cameraGo.transform.Translate(transform.forward * movementSpeed * -1, Space.Self);
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                cameraGo.transform.Translate(transform.right * movementSpeed * -1, Space.Self);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                cameraGo.transform.Translate(transform.right * movementSpeed, Space.Self);
            }
            if (Input.GetKeyDown(KeyCode.W))
            {
                mWireFrameCtrl.wireframeMode = !mWireFrameCtrl.wireframeMode;
            }
            if( Input.GetKeyDown(KeyCode.S))
            {
                renderGeoMappingCLOD = !renderGeoMappingCLOD; 
            }
        }
    }

    #endregion



    #region 更新及渲染
    public void DemoRender()
    {
        if (mGeoMappingTerrain != null)
        {
                    
            if( renderGeoMappingCLOD )
            {
                Profiler.BeginSample("QuadTree.Render");
                mGeoMappingTerrain.CLOD_Render(mGeoMappingTerrain.TerrainTexture,detailTexture,vertexScale);
                Profiler.EndSample();
            }
            else
            {
                Profiler.BeginSample("Normla.Render");
                mGeoMappingTerrain.Render(ref mMeshData, vertexScale);
                Profiler.EndSample();
            }
          
        }
    }

    public void InitMeshData()
    {
        if (null == terrainGo)
        {
            Debug.LogError("Terrain GameObject is Null");
            return;
        }

        MeshFilter meshFilter = terrainGo.GetComponent<MeshFilter>();
        if (null == meshFilter)
        {
            Debug.LogError("Terrain without Comp [MeshFilter]");
            return;
        }

        if (meshFilter.mesh == null)
        {
            meshFilter.mesh = new Mesh();
        }

       

        int vertexCnt = heightSize * heightSize;
        int trianglesCnt = (heightSize - 1) * (heightSize - 1) * 6; 
        mMeshData.mVertices = new Vector3[vertexCnt];
        mMeshData.mUV = new Vector2[vertexCnt];
        mMeshData.mNormals = new Vector3[vertexCnt];
        mMeshData.mTriangles = new int[trianglesCnt];
        mMeshData.mMesh = meshFilter.mesh;
    }



    public void InitRenderMode() 
    {
        if (cameraGo != null)
        {
            mWireFrameCtrl = cameraGo.GetComponent<RenderInWireframe>();
        }
    }

    #endregion


    // Update is called once per frame
    void Update ()
    {
        Profiler.BeginSample("DemoInput"); 
        DemoInput();
        Profiler.EndSample(); 

        Profiler.BeginSample("DemoRender"); 
        DemoRender();
        Profiler.EndSample(); 
    }

}
