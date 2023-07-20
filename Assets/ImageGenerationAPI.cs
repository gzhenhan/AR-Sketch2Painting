using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class ImageGenerationAPI : MonoBehaviour
{

    private byte[]  sourceImage;
    public Texture2D targetImage;
    public TMP_InputField tmpInputField;
    public RawImage streamedImage;

    public Vector3[] pointHistory;
    public Vector2[] projectedPoints;
    private Texture2D renderedTexture;
    public int textureWidth = 512; // Width of the output texture
    public int textureHeight = 512; // Height of the output texture
    public int pointSize;
    public float distanceThreshold = 1f; // Threshold distance to consider a point as an inlier
    public Vector3 planeNormal;

    private const string API_KEY = "6f534b76-0253-4f16-bf1c-2c5f78d00291";


    // Models
    private const string Stable_Diffusion_1_5 = "8b1b897c-d66d-45a6-b8d7-8e32421d02cf";
    private const string Stable_Diffusion_2_1 = "ee88d150-4259-4b77-9d0f-090abe29f650";
    private const string OpenJourney_v4 = "1e7737d7-545e-469f-857f-e4b46eaa151d";
    private const string OpenJourney_v2 = "d66b1686-5e5d-43b2-a2e7-d295d679917c";
    private const string OpenJourney_v1 = "7575ea52-3d4f-400f-9ded-09f7b1b1a5b8";
    private const string Modern_Disney = "8ead1e66-5722-4ff6-a13f-b5212f575321";
    private const string Future_Diffusion = "1285ded4-b11b-4993-a491-d87cdfe6310c";
    private const string Realistic_Vision_v2_0 =	"eab32df0-de26-4b83-a908-a83f3015e971";

    private const string modelId = Stable_Diffusion_2_1;
    private string startJobUrl = "https://api.tryleap.ai/api/v1/images/models/" + modelId + "/remix";
    private string jobStatusUrl = "https://api.tryleap.ai/api/v1/images/models/" + modelId + "/remix/";

    private string promptText = "";
    private string remixId = "";
    private string imageUri = "";
    
    private const int STATUS_INIT = 0;
    private const int STATUS_POINTCLOUD= 7;
    private const int STATUS_REQUEST_pointCloud = 8;
    private const int STATUS_REQUEST_JOB = 1;
    private const int STATUS_START_JOB = 2;
    private const int STATUS_REQUEST_CHECK_JOBSTATUS = 3;
    private const int STATUS_CHECKING_JOBSTATUS = 4;
    private const int STATUS_LOAD_IMAGE = 5;
    private const int STATUS_IMAGE_LOADING = 6;

    private int requestStatus = STATUS_INIT;
    private string jobStatus = "";

    public MarkerPosition mPosition;



    void Start()
    {
        tmpInputField.onEndEdit.AddListener(OnEndEditHandler);
        
        // Texture2D compressedTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/houseprompt.jpeg");
        // RenderTexture renderTexture = new RenderTexture(compressedTexture.width, compressedTexture.height, 0);
        // Graphics.Blit(compressedTexture, renderTexture);
        // sourceImage = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
        // RenderTexture.active = renderTexture;
        // sourceImage.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        // sourceImage.Apply();
    }

        private void OnEndEditHandler(string newValue)
    {
        promptText = newValue;
        tmpInputField.gameObject.SetActive(false);
        Debug.Log("Starting Coroutine with prompt: "+promptText);
        requestStatus = STATUS_REQUEST_pointCloud;
    }

    void Update()
    {

        // Vector3 drawBoradPos = mPosition.drawBorad.position;
        // Debug.Log("painter_pos in imagination: "+drawBoradPos);

        // requestStatusText.text = "STATUS: "+requestStatus+ " -- "+jobStatus;
        if(requestStatus == STATUS_REQUEST_JOB){
            requestStatus = STATUS_START_JOB;
            StartCoroutine(SendStartJobRequest());
        } else if (requestStatus == STATUS_REQUEST_CHECK_JOBSTATUS) {
            StartCoroutine(SendJobStatusRequest());
        } else if (requestStatus == STATUS_LOAD_IMAGE) {
            requestStatus = STATUS_IMAGE_LOADING;
            StartCoroutine(LoadImage());
        } else if(requestStatus == STATUS_REQUEST_pointCloud){
            requestStatus = STATUS_POINTCLOUD;
            StartCoroutine(ReqPointCloud());
        }

    }

    IEnumerator SendStartJobRequest()
    {
        WWWForm formData = new WWWForm();
        formData.AddField("prompt", promptText);
        // formData.AddField("width:", "1024");
        // formData.AddField("height:", "768");

        // byte[] imageData = sourceImage.EncodeToPNG();
        formData.AddBinaryData("files", sourceImage, "image.png", "image/png");

        UnityWebRequest request = UnityWebRequest.Post(startJobUrl, formData);

        // request.SetRequestHeader("content-type", "multipart/form-data");
        request.SetRequestHeader("Authorization", "Bearer " + API_KEY);

        yield return request.SendWebRequest();

        if (string.IsNullOrWhiteSpace(request.error))
        {
            string responseText = request.downloadHandler.text;
            APIResponse_JobRequest apiResponse = JsonUtility.FromJson<APIResponse_JobRequest>(responseText);
            remixId = apiResponse.id;
            requestStatus = STATUS_REQUEST_CHECK_JOBSTATUS;

        Debug.Log("SendStartJobRequest(): Request sent successfully. ID: " + remixId);
            
        }
        else
        {
            Debug.Log("SendStartJobRequest(): Error sending request: " + request.error);
            Debug.Log("SendStartJobRequest(): Message: " + request.downloadHandler.text);
            requestStatus = STATUS_INIT;
            tmpInputField.gameObject.SetActive(true);
        }
    }

    IEnumerator ReqPointCloud(){

        // get thw point cloud from the ARDraw component attached to the Manager game object
        ARDraw arDraw = GameObject.Find("Manager").GetComponent<ARDraw>();
        Vector3[] pointHistory = arDraw.pointHistory;
        Debug.Log("pointHistory: "+pointHistory);

        projectedPoints = ProjectPointCloud(pointHistory);
        Debug.Log("projectedPoints: "+projectedPoints);

        RenderPointCloud();


        yield return null;

    }

    public Vector2[] ProjectPointCloud(Vector3[] pointCloud)
    {
        // Estimate the normal plane using RANSAC
        planeNormal = EstimatePlaneNormal(pointCloud);

        // Project the point cloud onto the normal plane and store the 2D coordinates
        Vector2[] projectedPoints = new Vector2[pointCloud.Length];
        for (int i = 0; i < pointCloud.Length; i++)
        {
            Vector3 point = pointCloud[i];

            // Project the point onto the normal plane
            Vector3 projectedPoint = point - Vector3.Dot(point - pointCloud[0], planeNormal) * planeNormal;

            // Convert the 3D projected point to 2D coordinates in the normal plane
            Vector2 projectedPoint2D = new Vector2(projectedPoint.x, projectedPoint.y);

            projectedPoints[i] = projectedPoint2D;
        }

        return projectedPoints;
    }

        Vector3 EstimatePlaneNormal(Vector3[] pointCloud)
    {
        Vector3 normalSum = Vector3.zero;
        int numNeighbors = 0;

        int numPoints = pointCloud.Length;
        for (int i = 0; i < numPoints; i++)
        {
            if (pointCloud[i] != Vector3.zero)
            {
                Vector3 point = pointCloud[i];
                // Find neighboring points within the specified distance threshold
                for (int j = 0; j < numPoints; j++)
                {
                    if (i != j)
                    {
                        Vector3 neighbor = pointCloud[j];
                        float distance = Vector3.Distance(point, neighbor);

                        if (distance <= distanceThreshold)
                        {
                            // Calculate the surface normal between the point and its neighbor
                            Vector3 surfaceNormal = Vector3.Cross(point - neighbor, point - pointCloud[(j + 1) % numPoints]);
                            normalSum += surfaceNormal.normalized;
                            numNeighbors++;
                        }
                    }
                }
            }
            


        }

        // Calculate the average of the surface normals
        Vector3 planeNormal = normalSum / numNeighbors;

        return planeNormal.normalized;
    }

    void RenderPointCloud()
    {
        renderedTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        Color[] colors = new Color[textureWidth * textureHeight];

        // Clear the texture to a default color
        Color clearColor = Color.white;
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = clearColor;
        }

        // Scale and shift the projected points to fit within the texture bounds
        Vector2 minBounds = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 maxBounds = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 projectedPoint in projectedPoints)
        {
            minBounds.x = Mathf.Min(minBounds.x, projectedPoint.x);
            minBounds.y = Mathf.Min(minBounds.y, projectedPoint.y);
            maxBounds.x = Mathf.Max(maxBounds.x, projectedPoint.x);
            maxBounds.y = Mathf.Max(maxBounds.y, projectedPoint.y);
        }
        Vector2 scale = new Vector2(textureWidth / (maxBounds.x - minBounds.x), textureHeight / (maxBounds.y - minBounds.y));
        Vector2 shift = -minBounds;

        // Set the pixel values based on the projected point positions
        foreach (Vector2 point in projectedPoints)
        {
            Vector2 texturePos = Vector2.Scale(point + shift, scale);
            int x = Mathf.FloorToInt(texturePos.x);
            int y = Mathf.FloorToInt(texturePos.y);

            for (int i = -pointSize; i <= pointSize; i++)
            {
                for (int j = -pointSize; j <= pointSize; j++)
                {
                    int px = x + i;
                    int py = y + j;

                    // Check if the pixel is within the image boundaries
                    if (px >= 0 && px < textureWidth && py >= 0 && py < textureHeight)
                    {
                        // Set the pixel in the Texture2D to indicate the presence of a point
                        renderedTexture.SetPixel(px, py, Color.black);
                    }
                }
            }
        }

        // Apply the pixel values to the texture
        renderedTexture.Apply();


        // Convert the Texture2D to a byte array
        sourceImage = renderedTexture.EncodeToPNG();

        // Save the byte array as an image file (e.g., PNG)
        // System.IO.File.WriteAllBytes("assets/projected_image1.png", imageData);
        Debug.Log("done");
        requestStatus = STATUS_REQUEST_JOB;
    }

    IEnumerator SendJobStatusRequest()
    {
        requestStatus = STATUS_CHECKING_JOBSTATUS;
        while(requestStatus == STATUS_CHECKING_JOBSTATUS){
            UnityWebRequest request = UnityWebRequest.Get(jobStatusUrl+remixId);
            request.SetRequestHeader("Authorization", "Bearer " + API_KEY);
            request.SendWebRequest();
            
            yield return new WaitForSeconds(5);

            if (string.IsNullOrWhiteSpace(request.error))
            {
                string responseText = request.downloadHandler.text;
                Debug.Log("SendJobStatusRequest(): Response: " + responseText);

                APIResponse_JobStatus apiResponse = JsonUtility.FromJson<APIResponse_JobStatus>(responseText);
                string status = apiResponse.status;

                Debug.Log("SendJobStatusRequest(): Status: " + status);
                jobStatus = status;

                if (apiResponse.images != null && apiResponse.images.Length > 0)
                {
                    string uri = apiResponse.images[0].uri;
                    Debug.Log("SendJobStatusRequest(): URI: " + uri);
                    imageUri = uri;
                    requestStatus = STATUS_LOAD_IMAGE;
                }
            }
            else
            {
                Debug.Log("SendJobStatusRequest(): Error sending request: " + request.error);
            }
        }
    }

    IEnumerator LoadImage()
    {
        Debug.Log("LoadImage(): Loading Image: " + imageUri);
        UnityWebRequest www = UnityWebRequestTexture.GetTexture(imageUri);
        www.certificateHandler = new BypassCertificateHandler();
        yield return www.SendWebRequest();

        if (string.IsNullOrWhiteSpace(www.error))
        {
            Texture2D tex = ((DownloadHandlerTexture)www.downloadHandler).texture;
            
            
            // set streamed image active
            // streamedImage.gameObject.SetActive(true);
            // streamedImage.texture = tex;


            // show image in 3d world
            Texture2D image = tex;
            GameObject imageObject = new GameObject("ImportedImage");
            SpriteRenderer spriteRenderer = imageObject.AddComponent<SpriteRenderer>();

            // 创建 Sprite
            float multiplier = 1.0f;
            Rect spriteRect = new Rect(0, 0, image.width * multiplier, image.height * multiplier);
            Vector2 pivot = new Vector2(0.5f, 0.5f); // 设置图片的中心点
            Sprite sprite = Sprite.Create(image, spriteRect, pivot);

            // 设置 SpriteRenderer 的 Sprite
            spriteRenderer.sprite = sprite;
             Vector3 drawBoradPos = mPosition.drawBorad.position;
            // imageObject.transform.position = new Vector3(5, 0, 10);
             imageObject.transform.position = drawBoradPos;
             imageObject.transform.localScale = new Vector3(0.05f, 0.05f, 0.05f);





            targetImage = tex;
            requestStatus = STATUS_INIT;
        }
        else
        {
            Debug.Log("LoadImage(): Error Loading Image: " + www.error);
            requestStatus = STATUS_INIT;
        }
    }

    [System.Serializable]
    public class APIResponse_JobRequest
    {
        public string id;

    }

    [System.Serializable]
    public class APIResponse_JobStatus
    {
        public string status;
        public ImageData[] images;
    }

    [System.Serializable]
    public class ImageData
    {
        public string uri;
    }

    public class BypassCertificateHandler : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            // Bypass certificate validation
            return true;
        }
    }
}
