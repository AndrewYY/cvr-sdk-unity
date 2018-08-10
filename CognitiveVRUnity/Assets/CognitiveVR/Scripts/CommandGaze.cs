﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CognitiveVR;
using UnityEngine.Rendering;

//use command buffer
//TODO media

public class CommandGaze : GazeBase {

    public RenderTexture rt;
    public BuiltinRenderTextureType blitTo = BuiltinRenderTextureType.CurrentActive;
    public CameraEvent camevent = CameraEvent.BeforeForwardOpaque;

    CommandBufferHelper helper;

    public override void Initialize()
    {
        base.Initialize();
        CognitiveVR_Manager.InitEvent += CognitiveVR_Manager_InitEvent;
    }

    private void CognitiveVR_Manager_InitEvent(Error initError)
    {
        if (initError == Error.Success)
        {
            //should reparent to camera or add helper component?

            var buf = new CommandBuffer();
            buf.name = "cognitive depth";
            CameraComponent.depthTextureMode = DepthTextureMode.Depth;
            CameraComponent.AddCommandBuffer(camevent, buf);
            //stolen from debugview
            var material = new Material(Shader.Find("Hidden/Post FX/Builtin Debug Views"));
            //var settings = model.settings.depth;

            buf.SetGlobalFloat(Shader.PropertyToID("_DepthScale"), 1f / 1);
            buf.Blit((Texture)null, BuiltinRenderTextureType.CameraTarget, material, (int)0);
            //cam.AddCommandBuffer(CameraEvent.BeforeImageEffectsOpaque, buf);

            rt = new RenderTexture(256, 256, 24);
            

            buf.Blit(blitTo, rt);
            //buf.Blit(rt, (RenderTexture)null);

            //StartCoroutine(EndOfFrame());
            //if (debugImage != null)
            //    debugImage.texture = readTexture;


            CognitiveVR_Manager.TickEvent += CognitiveVR_Manager_TickEvent;

            helper = CameraTransform.gameObject.AddComponent<CommandBufferHelper>();
            helper.Initialize(rt, CameraComponent, OnHelperPostRender);
            //enable command buffer helper
            //let it do its thing
            //register a callback with parameters when it's finished

        }
    }

    private void CognitiveVR_Manager_TickEvent()
    {
        Vector3 viewport = GetViewportGazePoint();
        viewport.z = 100;
        var viewportray = CameraComponent.ViewportPointToRay(viewport);

        helper.Begin(GetViewportGazePoint(), viewportray);
    }

    void OnHelperPostRender(Ray ray, Vector3 gazepoint)
    {
        //RaycastHit hit = new RaycastHit();

        Vector3 gpsloc = new Vector3();
        float compass = 0;
        Vector3 floorPos = new Vector3();

        GetOptionalSnapshotData(ref gpsloc, ref compass, ref floorPos);

        float hitDistance;
        DynamicObject hitDynamic;
        Vector3 hitWorld;
        if (DynamicRaycast(ray.origin, ray.direction, CameraComponent.farClipPlane, 0.05f, out hitDistance, out hitDynamic, out hitWorld)) //hit dynamic
        {
            string ObjectId = hitDynamic.ObjectId.Id;
            Vector3 LocalGaze = hitDynamic.transform.InverseTransformPointUnscaled(hitWorld);
            hitDynamic.OnGaze(CognitiveVR_Preferences.S_SnapshotInterval);
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), ObjectId, LocalGaze, CameraTransform.position, CameraTransform.rotation, gpsloc, compass, floorPos);
            return;
        }

        if (gazepoint.magnitude > CameraComponent.farClipPlane * 0.99f) //compare to farplane. skybox
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos, rot, gpsloc, compass, floorPos);
            Debug.DrawRay(transform.position, transform.forward * CameraComponent.farClipPlane, Color.cyan, 1);
        }
        else
        {
            Vector3 pos = CameraTransform.position;
            Quaternion rot = CameraTransform.rotation;

            //hit world
            GazeCore.RecordGazePoint(Util.Timestamp(Time.frameCount), pos+gazepoint, pos, rot, gpsloc, compass, floorPos);
            Debug.DrawLine(pos, pos + gazepoint, Color.red, 1);
            LastGazePoint = pos + gazepoint;
        }
    }

    /*private void OnDrawGizmos()
    {
        UnityEditor.Handles.BeginGUI();
        GUI.Label(new Rect(0, 0, 128, 128), rt);
        UnityEditor.Handles.EndGUI();
    }*/

    private void OnDestroy()
    {
        Destroy(helper);
        CognitiveVR_Manager.InitEvent -= CognitiveVR_Manager_InitEvent;
        CognitiveVR_Manager.TickEvent -= CognitiveVR_Manager_TickEvent;
    }
}
