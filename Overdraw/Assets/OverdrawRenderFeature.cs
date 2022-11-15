using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OverdrawRenderFeature : ScriptableRendererFeature
{
    class OverdrawRenderPass : ScriptableRenderPass
    {
        private Overdraw _overdraw;

        private string _passCmdName;
        private bool _isOpaque;
        private List<ShaderTagId> _tagIdList = new List<ShaderTagId>();
        private FilteringSettings _filteringSettings;
        private Material _material;
        
        public OverdrawRenderPass(string passCmdName, RenderQueueRange renderQueueRange, RenderPassEvent evt, Shader shader, bool isOpaque)
        {
            _passCmdName = passCmdName;
            _isOpaque = isOpaque;
            
            _tagIdList.Add(new ShaderTagId("UniversalForward"));
            _tagIdList.Add(new ShaderTagId("LightweightForward"));
            _tagIdList.Add(new ShaderTagId("SRPDefaultUnlit"));
            _filteringSettings = new FilteringSettings(renderQueueRange, LayerMask.NameToLayer("Everything"));

            _material = CoreUtils.CreateEngineMaterial(shader);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            _overdraw = stack.GetComponent<Overdraw>();
            if (_overdraw == null)
            {
                return;
            }

            if (!_overdraw.IsActive())
            {
                return;
            }

            var cmd = CommandBufferPool.Get(_passCmdName);

            #region Render

            var camera = renderingData.cameraData.camera;
            if (_isOpaque)
            {
                if (renderingData.cameraData.isSceneViewCamera ||
                    (camera.TryGetComponent(out UniversalAdditionalCameraData urpCameraData) &&
                     urpCameraData.renderType == CameraRenderType.Base))
                {
                    cmd.ClearRenderTarget(true, true, Color.black);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            var sortFlags = _isOpaque
                ? renderingData.cameraData.defaultOpaqueSortFlags
                : SortingCriteria.CommonTransparent;
            var drawSettings = CreateDrawingSettings(_tagIdList, ref renderingData, sortFlags);
            drawSettings.overrideMaterial = _material;
            context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
            #endregion
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

       
    }

    OverdrawRenderPass opaquePass;
    OverdrawRenderPass transparentPass;
    
    [SerializeField] private Shader opaqueShader = null;
    [SerializeField] private Shader transparentShader = null;
    
    public override void Create()
    {
        if (!opaqueShader || !transparentShader)
        {
            return;
        }

        opaquePass = new OverdrawRenderPass("Overdraw Render Opaque", RenderQueueRange.opaque,
            RenderPassEvent.AfterRenderingSkybox, opaqueShader, true);
        transparentPass = new OverdrawRenderPass("Overdraw Render Transoarent", RenderQueueRange.transparent,
            RenderPassEvent.AfterRenderingTransparents, transparentShader, false);
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(opaquePass);
        renderer.EnqueuePass(transparentPass);
    }
}
