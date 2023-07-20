using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO;

public class ARDraw : MonoBehaviour
{
    //线条的预制件
    public GameObject linePrefab;
    //主摄像头
    private Camera targetCamera;
    private LineRenderer lineRenderer;
    //线段位置离屏幕的距离
    public float drawOffset;
    //绘画状态
    private bool isDrawing;
    //线段宽度
    public float lineWidth;
    //实例化的线条
    private GameObject lineClone;
    //记录当前线段数
    private int lineCounter;
     //记录所有线段数
    private int allLineCounter;
    //绘画的根结点
    public GameObject drawRoot;
    //当前使用的材质
    private Material currentMaterial;
    //颜色选择UI
    public GameObject colorPopup;
    //材质选择ui
    public GameObject materialPopup;

    public Transform painter;
    

    public MarkerPosition mPosition;
    
    public Vector3[] pointHistory;
    public int maxPointCount = 1000;

  

    // Start is called before the first frame
    void Start()
    {
        targetCamera = Camera.main;
        //依据触摸的状态，确定绘画的状态 store the drawing status
        isDrawing = false;
        //默认加载白色材质
        currentMaterial = Resources.Load("Materials/White", typeof(Material)) as Material;
        lineCounter = 0;
        allLineCounter = 0;
        


        pointHistory = new Vector3[maxPointCount];
        // markerPosition = GetComponentInChildren<MarkerPosition>();
        // if(markerPosition.painter == null){
        //     Debug.Log("painter is null !");
        // }

    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("update AR Draw");
        if (Input.touchCount > 0)
        {
            if(!IsPointerOverUIObject())
            {
                Debug.Log("not over UI");
                //close popups
                ClosePopups();

                Touch touch = Input.GetTouch(0);
                switch (touch.phase)
                {
                    // 记录初始的点击位置
                    case TouchPhase.Began:
                        lineClone = (GameObject)Instantiate(linePrefab);
                        //lineClone.tag = "ARLines";
                        lineClone.GetComponent<LineRenderer>().material = currentMaterial;
                        lineClone.transform.parent = drawRoot.transform;
                        lineRenderer = lineClone.GetComponent<LineRenderer>();
                        lineRenderer.startWidth = lineWidth;
                        lineRenderer.endWidth = lineWidth;
                        isDrawing = true;
                        lineCounter = 0;
                        break;
                    case TouchPhase.Moved:
                        break;
                    case TouchPhase.Ended:
                        isDrawing = false;
                        break;
                }

                if (isDrawing)
                {
                    Debug.Log("is drawing");
                    lineCounter++;
                    lineRenderer.positionCount = lineCounter;
                    allLineCounter++;
                    //直接已摄像头的位置确定线段位置
                    //line.SetPosition(i - 1, targetCamera.transform.position + drawOffset * targetCamera.transform.forward);
                    //依据屏幕触摸位置确定线段位置
                    // lineRenderer.SetPosition(lineCounter - 1, targetCamera.ScreenToWorldPoint(new Vector3(touch.position.x, touch.position.y, drawOffset)));
                    // lineRenderer.SetPosition(lineCounter - 1, targetCamera.ScreenToWorldPoint(new Vector3(mPosition.painter.position.x, mPosition.painter.position.y, drawOffset)));

                    // Vector3 painter2drawboard = mPosition.painter.position-mPosition.drawBorad.position;
                     Vector3 painter2drawboard = mPosition.painter.position;

                    lineRenderer.SetPosition(lineCounter - 1, painter2drawboard);
                    // save the point in world coordinate
                    pointHistory[allLineCounter - 1] = mPosition.painter.position;

                    // save the point in screen coordinate
                    // Vector3 screenPos = targetCamera.WorldToScreenPoint(painter2drawboard);
                    // pointHistory[allLineCounter - 1] = screenPos;

                }

                if(isDrawing == false){
                    // if(allLineCounter > 0){
                    // SavePointsToFile();
                    // }
                }

                


            }
        }

    }

    //判断当前点击位置是否是UI组件，避免在点击按钮时，还继续画画
    private bool IsPointerOverUIObject()
    {
        //判断是否点击的是UI，有效应对安卓没有反应的情况，true为UI
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);
        return results.Count > 0;
    }




    // 保存painter相对于drawboard的轨迹

    public void SavePointsToFile()
    {
        string filePath = Path.Combine(Application.persistentDataPath, "linePoints_checker.txt");


        Debug.Log("start saving points to file");
        int pointCount =pointHistory.Length;
        Debug.Log("point count: " + pointCount);
        // Vector3[] points = new Vector3[pointCount];
        // lineRenderer.GetPositions(points);

        // string[] lines = new string[pointCount];
        for (int i = 0; i < pointCount; i++)
        {
            Debug.Log("see points: " + pointHistory[i].ToString()); 
        }
        // Debug.Log("lines count: " + lines.Length);

        // File.WriteAllLines(filePath, lines);
        using (StreamWriter writer = new StreamWriter(filePath))
        {
            for (int i = 0; i < pointCount; i++)
            {
                writer.WriteLine(pointHistory[i].ToString("F3"));
            }
        }



        Debug.Log("Line points saved to file: " + filePath);
    }

    //清除线段
    public void Clean()
    {
        int childCount = drawRoot.transform.childCount;
        for (int j = 0; j < childCount; j++)
        {
            Destroy(drawRoot.transform.GetChild(j).gameObject);
        }
        ClosePopups();
    }

    //通过名称在资源文件夹中查找材质，然后改变当前材质
    public void ChangeTexure(string materialName)
    {
        currentMaterial = (Material)Instantiate(Resources.Load("Materials/" + materialName, typeof(Material)) as Material);
        ClosePopups();
    }

    //直接通过传入材质，改变当前材质
    public void ChangeTexure(Material material)
    {
        //SetMaterialRenderingMode(material, RenderingMode.Transparent);
        currentMaterial = (Material)Instantiate(material);
        ClosePopups();
    }

    public void ToggleColorPopup()
    {
        ClosePopups();
        colorPopup.SetActive(!colorPopup.activeSelf);
    }

    public void ToggleMaterialPopup()
    {
        ClosePopups();
        materialPopup.SetActive(!materialPopup.activeSelf);
    }

    public void ClosePopups()
    {
        //close popups
        materialPopup.SetActive(false);
        colorPopup.SetActive(false);
    }
}
