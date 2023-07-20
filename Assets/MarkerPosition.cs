
﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class MarkerPosition : MonoBehaviour

{

    private ARTrackedImageManager _arTrackedImageManager;
    
    public GameObject prefabMarker_01;
    public GameObject prefabMarker_02;
    private GameObject spawnedPrefab;

    public class ImageData
    {
        public string name;
        public Transform transform;
    }
    public ImageData imageData ;
    public TMP_Text canvasText;
    public Transform[] imagePositions;
    public List<ImageData> uniqueTransformSet;
    public Transform painter;
    public Transform drawBorad;

    private string[] imageLabels; // Array to store image labels
    private const string defaultText = "Image Position: N/A";



    private void Awake()
    {
        _arTrackedImageManager = FindObjectOfType<ARTrackedImageManager>();
    }

    public void OnEnable()
    {
 
        _arTrackedImageManager.trackedImagesChanged += OnImageChanged;
    }

    public void OnDisable()
    {
        _arTrackedImageManager.trackedImagesChanged -= OnImageChanged;
    }

    void UpdateInfo(ARTrackedImage trackedImage)
    {
        // Update information about the tracked image
        canvasText.text = string.Format(
            "{0}\ntrackingState: {1}\nGUID: {2}\nReference size: {3} cm\nDetected size: {4} cm\nPosition: {5}",
            trackedImage.referenceImage.name,
            trackedImage.trackingState,
            trackedImage.referenceImage.guid,
            trackedImage.referenceImage.size * 100f,
            trackedImage.size * 100f,
            trackedImage.transform.position);

        var planeParentGo = trackedImage.transform.GetChild(0).gameObject;
        var planeGo = planeParentGo.transform.GetChild(0).gameObject;

        // Disable the visual plane if it is not being tracked

        planeGo.SetActive(true);

    }

    public void OnImageChanged(ARTrackedImagesChangedEventArgs args)
    {

        foreach (var trackedImage in args.added)
        {
            
        }

        foreach (var trackedImage in args.updated)
        {
          
        }

        foreach (var trackedImage in args.removed)
        {
        }



    }

    private void Start()
    {

        // intialize drawBorad
        // drawBorad = GameObject.Find("DrawBoard").transform;
  
    
        // Initialize the image labels array
        imageLabels = new string[2];
        imagePositions = new Transform[imageLabels.Length];
        uniqueTransformSet = new List<ImageData>();

        // Set the default text for all image labels
        for (int i = 0; i < 2; i++)
        {
            imageLabels[i] = defaultText;
        }
    }

    private void Update()
    {


        Debug.Log("Number of Tracked Images: " + _arTrackedImageManager.trackables.count);
        int index = 0; 

        foreach (ARTrackedImage trackedImage in _arTrackedImageManager.trackables)
        {
            Debug.Log("Tracked Image Ref Name: "+trackedImage.referenceImage.name + " -- "+ trackedImage.trackingState);


             if( painter!=null ){
                // GameObject extraPrefabs= trackedImage.transform.GetChild(0).gameObject;
                // extraPrefabs.SetActive(true);
                // extraPrefabs.transform.GetChild(0).gameObject.SetActive(true);
                if(trackedImage.referenceImage.name == "Marker_01"){
                        painter.position = trackedImage.transform.position;
                        trackedImage.transform.GetChild(0).gameObject.SetActive(true);
                        Debug.Log("Child 0 Name: "+ trackedImage.transform.GetChild(0).gameObject.name);
                    
                } else if(trackedImage.referenceImage.name == "Marker_02"){
                        drawBorad.position = trackedImage.transform.position;
                        // check  if the type of gameobject is a 3D text
                        trackedImage.transform.GetChild(1).gameObject.SetActive(true);
                        Debug.Log("Child 1 Name: "+ trackedImage.transform.GetChild(1).gameObject.name);

                        // GameObject text= trackedImage.transform.GetChild(1).gameObject;
                        // TextMesh textMesh = text.GetComponent<TextMesh>();
                        // if (textMesh != null)
                        // {
                        //     textMesh.text = trackedImage.referenceImage.name ;
                        // }
                  }

                }





            uniqueTransformSet.Clear();
            // Debug.Log(trackedImage.transform.position.ToString());
            imageData = new ImageData();
            imageData.name = trackedImage.referenceImage.name;
            imageData.transform = trackedImage.transform;
            uniqueTransformSet.Add(imageData);
            // if (index < 2)
            // {
            //     Debug.Log(index);
            //     imageLabels[index] = trackedImage.transform.position.ToString();
            //     Debug.Log(trackedImage.transform.position.ToString());
            //     imageLabels[index] = "Image " + (index + 1) + " Position: " + trackedImage.transform.position.ToString();
            //     index++; 
            // }
            // Disable the visual plane if it is not being tracked

        }

        int i = 0;
        foreach (ImageData imageData in uniqueTransformSet)
        {
            // Debug.Log(imageData.name);
            // Debug.Log(imageData.transform.position.ToString());
            imageLabels[i]= "Image "+ imageData.name + " Position: " + imageData.transform.position.ToString();
            i++;
        }
        // imageLabels[0] = "Image "+ "Marker_02" + " Position: " +GetTransformByName("Marker_02");
        canvasText.text = string.Join("\n", imageLabels);
    }







    private void SpawnPrefab(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject spawnedPrefab = Instantiate(prefab, position, rotation);
        // Customize the spawnedPrefab as desired
    }


   

    private string GetTransformByName(string name)
    {
        foreach (ImageData imageData in uniqueTransformSet)
        {
            if (imageData.name == name)
            {
                return imageData.transform.position.ToString();
            }
        }
        return null;
    }
    


}
