using System.Numerics;
using ImGuiNET;
using Vault.Input.Mouse;
using VaultCore.Rendering;

namespace Vault;

public class VaultCoreSoftwareRendering : ISoftwareRendering
{
    private class RenderOutputData
    {
        public readonly RendererOutputWindow OutputWindow;
        public readonly string OutputName;
        public Texture2D? RenderOutput;

        public RenderOutputData(string outputName, RendererOutputWindow outputWindow)
        {
            OutputWindow = outputWindow;
            OutputName = outputName;
        }
    }


    private readonly TextureManager _textureManager;
    private readonly VaultGui _parentGuiApplication;
    private readonly ImGuiUiManager _imGuiManager;
    private readonly Logger _logger;

    private readonly Dictionary<RenderOutputHandle, RenderOutputData> _outputs = new();

    public VaultCoreSoftwareRendering(Logger logger, TextureManager textureManager, ImGuiUiManager imGuiManager, VaultGui parentGuiApplication)
    {
        _logger = logger;
        _textureManager = textureManager;
        _imGuiManager = imGuiManager;
        _parentGuiApplication = parentGuiApplication;
    }

    public void OnCoreAcquiresFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded) { }

    public void OnCoreReleasesFeature(Type coreType, Type featureType, List<Type> allCoreFeaturesNeeded)
    {
        if(_outputs.Count > 0)
        {
            _logger.LogWarning("ISoftwareRenderer been released by core, but core still has outputs active. " +
                               "They will be cleaned up, but Core should destroy all outputs before shutting down.");

            var existingHandles = _outputs.Keys.ToList();

            foreach (var handle in existingHandles)
            {
                _logger.LogWarning("Output Not Destroyed Before Core Shutdown: " + _outputs[handle].OutputName);
                DestroyOutput(handle);
            }
        }
    }

    public RenderOutputHandle CreateOutput(string outputName)
    {
        var newHandle = RenderOutputHandle.Create();

        var newOutputWindow = new RendererOutputWindow(outputName, _textureManager,
            _imGuiManager.ImGuiWindowManager, _parentGuiApplication);

        _imGuiManager.ImGuiWindowManager.RegisterWindow(newOutputWindow);

        _outputs.Add(newHandle, new RenderOutputData(outputName, newOutputWindow));

        return newHandle;
    }

    public void DestroyOutput(RenderOutputHandle handle)
    {
        if(handle == RenderOutputHandle.InvalidHandle)
        {
            throw new InvalidOperationException("Trying to use invalid handle");
        }
        
        if(_outputs.TryGetValue(handle, out var output) == false)
        {
            throw new InvalidOperationException("Trying to destroy output that does not exist");
        }

        _imGuiManager.ImGuiWindowManager.UnregisterWindow(output.OutputWindow);

        output.OutputWindow.Dispose();

        if(output.RenderOutput != null)
        {
            output.RenderOutput.Dispose();
        }

        _outputs.Remove(handle);
    }

    public void ResetOutput(RenderOutputHandle handle)
    {
        if(handle == RenderOutputHandle.InvalidHandle)
        {
            throw new InvalidOperationException("Trying to use invalid handle");
        }
        
        if(_outputs.TryGetValue(handle, out var output) == false)
        {
            throw new InvalidOperationException("Trying to use output that does not exist");
        }
        
        if(output.RenderOutput != null)
        {
            output.RenderOutput.Dispose();
            output.RenderOutput = null;
        }

        output.OutputWindow.SetTextureToShowOnScreen(null);
    }

    public Vector2 GetOutputSize(RenderOutputHandle handle)
    {
        if(handle == RenderOutputHandle.InvalidHandle)
        {
            throw new InvalidOperationException("Trying to use invalid handle");
        }
        
        if(_outputs.TryGetValue(handle, out var output) == false)
        {
            throw new InvalidOperationException("Trying to use output that does not exist");
        }
        
        return output.OutputWindow.ContentAreaSize;
    }

    public bool GetMouseAbsolutePosition(RenderOutputHandle handle, IMouseDevice mouseDevice, out Vector2 mousePosOut)
    {
        if(handle == RenderOutputHandle.InvalidHandle)
        {
            throw new InvalidOperationException("Trying to use invalid handle");
        }
        
        if(_outputs.TryGetValue(handle, out var output) == false)
        {
            throw new InvalidOperationException("Trying to use output that does not exist");
        }
        
        var mousePos = ImGui.GetMousePos();
        
        mousePosOut = new Vector2(
            (mousePos.X - output.OutputWindow.ContentAreaTopLeft.X) / output.OutputWindow.ContentAreaSize.X,
            (mousePos.Y - output.OutputWindow.ContentAreaTopLeft.Y) / output.OutputWindow.ContentAreaSize.Y);
        
        return mousePosOut.X >= 0.0f && mousePosOut.X <= 1.0f &&
               mousePosOut.Y >= 0.0f && mousePosOut.Y <= 1.0f;
    }

    public void OnFrameReadyToDisplayOnOutput(RenderOutputHandle target, PixelData pixelData)
    {
        if(target == RenderOutputHandle.InvalidHandle)
        {
            throw new InvalidOperationException("Trying to use invalid handle");
        }
        
        if(_outputs.TryGetValue(target, out var output) == false)
        {
            throw new InvalidOperationException("Trying to use output that does not exist");
        }

        //Init the output texture as needed
        if(output.RenderOutput == null ||
           output.RenderOutput.Width != pixelData.Width ||
           output.RenderOutput.Height != pixelData.Height)
        {
            if(output.RenderOutput != null)
            {
                output.RenderOutput.Dispose();
            }

            output.RenderOutput = _textureManager.CreateTexture(
                pixelData.Width,
                pixelData.Height,
                TextureFormat.RGBA_32,
                false,
                true);

            output.OutputWindow.SetTextureToShowOnScreen(output.RenderOutput);
        }

        //and just copy the software renderer output into the texture for displaying by the output window
        output.RenderOutput.StartWritingPixelsToTexture();
        output.RenderOutput.WritePixelData(pixelData.Data, pixelData.Width, pixelData.Height, 0, 0);
        output.RenderOutput.FinishWritingPixelsToTexture();
    }
}