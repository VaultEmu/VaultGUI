using System.Numerics;
using System.Runtime.CompilerServices;
using Vault;
using Veldrid;

namespace ImGuiNET;

/// <summary>
///     A modified version of Veldrid.ImGui's ImGuiRenderer.
///     Manages input for ImGui and handles rendering ImGui's DrawLists with Veldrid.
/// </summary>
/// Taken from github repo with further tweaks by me - https://github.com/mellinoe/ImGui.NET/commits/master/src/ImGui.NET.SampleProgram/ImGuiController.cs
public class ImGuiRenderer : IDisposable
{
    private readonly GraphicsDevice _gd;

    // Veldrid objects
    private DeviceBuffer _vertexBuffer;
    private DeviceBuffer _indexBuffer;
    private readonly DeviceBuffer _projMatrixBuffer;
    private readonly Texture _fontTexture;
    private readonly TextureView _fontTextureView;
    private readonly Shader _vertexShader;
    private readonly Shader _fragmentShader;
    private readonly ResourceLayout _layout;
    private readonly ResourceLayout _textureLayout;
    private readonly Pipeline _pipeline;
    private readonly ResourceSet _mainResourceSet;
    private readonly ResourceSet _fontTextureResourceSet;

    private readonly IntPtr _fontAtlasId = (IntPtr)1;

    private int _windowWidth;
    private int _windowHeight;
    private readonly Vector2 _scaleFactor = Vector2.One;

    // Image trackers
    private readonly Dictionary<TextureView, ResourceSetInfo> _setsByView = new();
    private readonly Dictionary<Texture, TextureView> _autoViewsByTexture = new();
    private readonly Dictionary<IntPtr, ResourceSetInfo> _viewsById = new();
    private readonly List<IDisposable> _ownedResources = new();
    private int _lastAssignedId = 100;

    /// <summary>
    ///     Constructs a new ImGuiRenderer.
    /// </summary>
    public ImGuiRenderer(GraphicsDevice gd, OutputDescription outputDescription, int width, int height, float dpiScale)
    {
        _gd = gd;
        _windowWidth = width;
        _windowHeight = height;
        
        var factory = gd.ResourceFactory;

        var context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        
        VertexLayoutDescription[] vertexLayouts =
        {
            new(
                new VertexElementDescription("in_position", VertexElementSemantic.Position, VertexElementFormat.Float2),
                new VertexElementDescription("in_texCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2),
                new VertexElementDescription("in_color", VertexElementSemantic.Color, VertexElementFormat.Byte4_Norm))
        };

        _layout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("ProjectionMatrixBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
            new ResourceLayoutElementDescription("MainSampler", ResourceKind.Sampler, ShaderStages.Fragment)));
        
        _textureLayout = factory.CreateResourceLayout(new ResourceLayoutDescription(
            new ResourceLayoutElementDescription("MainTexture", ResourceKind.TextureReadOnly, ShaderStages.Fragment)));
        
        
        unsafe
        {
            //Load the main Fonts
            var fontSize = (float)Math.Floor(15.0f * dpiScale);

            var fontAwesomeIconRange = new ushort[]
            {
                Fonts.FontAwesomeCodes.IconMin,
                Fonts.FontAwesomeCodes.IconMax,
                0
            };
            
            var mainFontData = File.ReadAllBytes(@".\Assets\Fonts\JetBrainsMonoNL-Medium.ttf");
            var fontAwesomeFontData = File.ReadAllBytes(@".\Assets\Fonts\fa-solid-900.ttf");
            
            fixed (byte* mainFontDataPtr = mainFontData)
            fixed (byte* fontAwesomeFontDataPtr = fontAwesomeFontData)
            fixed (ushort* rangesPtr = fontAwesomeIconRange)
            {
                ImGui.GetIO().Fonts.Clear();
                ImGui.GetIO().Fonts.AddFontFromMemoryTTF((IntPtr)mainFontDataPtr, mainFontData.Length, fontSize);
                
                var config = ImGuiNative.ImFontConfig_ImFontConfig();
                config->MergeMode = 1;
                config->GlyphMinAdvanceX = fontSize;

                //Merge icons into main font
                ImGui.GetIO().Fonts.AddFontFromMemoryTTF((IntPtr)fontAwesomeFontDataPtr, fontAwesomeFontData.Length, 
                    fontSize, config, (IntPtr)rangesPtr);             // Merge into first font
                
                //And Generate
                ImGui.GetIO().Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out var fontTextureWidth, out var fontTextureHeight, out var fontTextureBytesPerPixel);
                ImGui.GetIO().Fonts.SetTexID(_fontAtlasId);

                _fontTexture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                    (uint)fontTextureWidth,
                    (uint)fontTextureHeight,
                    1,
                    1,
                    PixelFormat.R8_G8_B8_A8_UNorm,
                    TextureUsage.Sampled));
                _fontTexture.Name = "ImGui.NET Font Texture";
        
                _gd.UpdateTexture(
                    _fontTexture,
                    pixels,
                    (uint)(fontTextureBytesPerPixel * fontTextureWidth * fontTextureHeight),
                    0,
                    0,
                    0,
                    (uint)fontTextureWidth,
                    (uint)fontTextureHeight,
                    1,
                    0,
                    0);
        
                _fontTextureView = _gd.ResourceFactory.CreateTextureView(_fontTexture);

                ImGui.GetIO().Fonts.ClearTexData();
                
                _fontTextureResourceSet = _gd.ResourceFactory.CreateResourceSet(
                    new ResourceSetDescription(_textureLayout, _fontTextureView));
            }
        }

        _vertexBuffer = factory.CreateBuffer(new BufferDescription(10000, BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        _vertexBuffer.Name = "ImGui.NET Vertex Buffer";
        _indexBuffer = factory.CreateBuffer(new BufferDescription(2000, BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        _indexBuffer.Name = "ImGui.NET Index Buffer";

        _projMatrixBuffer = factory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer | BufferUsage.Dynamic));
        _projMatrixBuffer.Name = "ImGui.NET Projection Buffer";

        var vertexShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-vertex");
        var fragmentShaderBytes = LoadEmbeddedShaderCode(gd.ResourceFactory, "imgui-frag");
        _vertexShader = factory.CreateShader(new ShaderDescription(ShaderStages.Vertex, vertexShaderBytes,
            gd.BackendType == GraphicsBackend.Metal ? "VS" : "main"));
        _fragmentShader =
            factory.CreateShader(new ShaderDescription(ShaderStages.Fragment, fragmentShaderBytes, gd.BackendType == GraphicsBackend.Metal ? "FS" : "main"));



        var pd = new GraphicsPipelineDescription(
            BlendStateDescription.SingleAlphaBlend,
            new DepthStencilStateDescription(false, false, ComparisonKind.Always),
            new RasterizerStateDescription(FaceCullMode.None, PolygonFillMode.Solid, FrontFace.Clockwise, false, true),
            PrimitiveTopology.TriangleList,
            new ShaderSetDescription(vertexLayouts, new[] { _vertexShader, _fragmentShader }),
            new[] { _layout, _textureLayout },
            outputDescription,
            ResourceBindingModel.Default);
        _pipeline = factory.CreateGraphicsPipeline(ref pd);

        _mainResourceSet = factory.CreateResourceSet(new ResourceSetDescription(_layout,
            _projMatrixBuffer,
            gd.PointSampler));
        
        ImGui.GetStyle().ScaleAllSizes(dpiScale);
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    /// <summary>
    ///     Gets or creates a handle for a texture to be drawn with ImGui.
    ///     Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, TextureView textureView)
    {
        if(!_setsByView.TryGetValue(textureView, out var rsi))
        {
            var resourceSet = factory.CreateResourceSet(new ResourceSetDescription(_textureLayout, textureView));
            rsi = new ResourceSetInfo(GetNextImGuiBindingId(), resourceSet);

            _setsByView.Add(textureView, rsi);
            _viewsById.Add(rsi.ImGuiBinding, rsi);
            _ownedResources.Add(resourceSet);
        }

        return rsi.ImGuiBinding;
    }

    private IntPtr GetNextImGuiBindingId()
    {
        var newId = _lastAssignedId++;
        return (IntPtr)newId;
    }

    /// <summary>
    ///     Gets or creates a handle for a texture to be drawn with ImGui.
    ///     Pass the returned handle to Image() or ImageButton().
    /// </summary>
    public IntPtr GetOrCreateImGuiBinding(ResourceFactory factory, Texture texture)
    {
        if(!_autoViewsByTexture.TryGetValue(texture, out var textureView))
        {
            textureView = factory.CreateTextureView(texture);
            _autoViewsByTexture.Add(texture, textureView);
            _ownedResources.Add(textureView);
        }

        return GetOrCreateImGuiBinding(factory, textureView);
    }

    /// <summary>
    ///     Retrieves the shader texture binding for the given helper handle.
    /// </summary>
    public ResourceSet GetImageResourceSet(IntPtr imGuiBinding)
    {
        if(!_viewsById.TryGetValue(imGuiBinding, out var tvi))
        {
            throw new InvalidOperationException("No registered ImGui binding with id " + imGuiBinding);
        }

        return tvi.ResourceSet;
    }

    public void ClearCachedImageResources()
    {
        foreach (var resource in _ownedResources)
        {
            resource.Dispose();
        }

        _ownedResources.Clear();
        _setsByView.Clear();
        _viewsById.Clear();
        _autoViewsByTexture.Clear();
        _lastAssignedId = 100;
    }

    private byte[] LoadEmbeddedShaderCode(ResourceFactory factory, string name)
    {
        switch (factory.BackendType)
        {
            case GraphicsBackend.Direct3D11:
            {
                var resourceName = name + ".hlsl.bytes";
                return GetEmbeddedResourceBytes(resourceName);
            }
            case GraphicsBackend.OpenGL:
            {
                var resourceName = name + ".glsl";
                return GetEmbeddedResourceBytes(resourceName);
            }
            case GraphicsBackend.Vulkan:
            {
                var resourceName = name + ".spv";
                return GetEmbeddedResourceBytes(resourceName);
            }
            case GraphicsBackend.Metal:
            {
                var resourceName = name + ".metallib";
                return GetEmbeddedResourceBytes(resourceName);
            }
            default:
                throw new NotImplementedException();
        }
    }

    private byte[] GetEmbeddedResourceBytes(string resourceName)
    {
        var assembly = typeof(ImGuiRenderer).Assembly;
        using (var s = assembly.GetManifestResourceStream(resourceName))
        {
            if(s == null)
            {
                throw new InvalidDataException($"Unable to load embedded resource stream {resourceName} from assembly");
            }
            
            var ret = new byte[s.Length];
            var numBytesToRead = (int)s.Length;
            
            while (numBytesToRead > 0)
            {
                int n = s.Read(ret, 0, (int)s.Length);
                numBytesToRead -= n;
            }

            return ret;
        }
    }
    
    public void OnNewFrameStart()
    {
        var io = ImGui.GetIO();
        
        //Update render values in IO
        io.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        
        ImGui.NewFrame();
    }

    /// <summary>
    ///     Renders the ImGui draw list data.
    ///     This method requires a <see cref="GraphicsDevice" /> because it may create new DeviceBuffers if the size of vertex
    ///     or index data has increased beyond the capacity of the existing buffers.
    ///     A <see cref="CommandList" /> is needed to submit drawing and resource update commands.
    /// </summary>
    public void Render(GraphicsDevice gd, CommandList cl)
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData(), gd, cl);
    }

    private void RenderImDrawData(ImDrawDataPtr drawData, GraphicsDevice gd, CommandList cl)
    {
        uint vertexOffsetInVertices = 0;
        uint indexOffsetInElements = 0;

        if(drawData.CmdListsCount == 0)
        {
            return;
        }

        var totalVbSize = (uint)(drawData.TotalVtxCount * Unsafe.SizeOf<ImDrawVert>());
        if(totalVbSize > _vertexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_vertexBuffer);
            _vertexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalVbSize * 1.5f), BufferUsage.VertexBuffer | BufferUsage.Dynamic));
        }

        var totalIbSize = (uint)(drawData.TotalIdxCount * sizeof(ushort));
        if(totalIbSize > _indexBuffer.SizeInBytes)
        {
            gd.DisposeWhenIdle(_indexBuffer);
            _indexBuffer = gd.ResourceFactory.CreateBuffer(new BufferDescription((uint)(totalIbSize * 1.5f), BufferUsage.IndexBuffer | BufferUsage.Dynamic));
        }

        for (var i = 0; i < drawData.CmdListsCount; i++)
        {
            var cmdList = drawData.CmdListsRange[i];

            cl.UpdateBuffer(
                _vertexBuffer,
                vertexOffsetInVertices * (uint)Unsafe.SizeOf<ImDrawVert>(),
                cmdList.VtxBuffer.Data,
                (uint)(cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>()));

            cl.UpdateBuffer(
                _indexBuffer,
                indexOffsetInElements * sizeof(ushort),
                cmdList.IdxBuffer.Data,
                (uint)(cmdList.IdxBuffer.Size * sizeof(ushort)));

            vertexOffsetInVertices += (uint)cmdList.VtxBuffer.Size;
            indexOffsetInElements += (uint)cmdList.IdxBuffer.Size;
        }

        // Setup orthographic projection matrix into our constant buffer
        var io = ImGui.GetIO();
        var mvp = Matrix4x4.CreateOrthographicOffCenter(
            0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        _gd.UpdateBuffer(_projMatrixBuffer, 0, ref mvp);

        cl.SetVertexBuffer(0, _vertexBuffer);
        cl.SetIndexBuffer(_indexBuffer, IndexFormat.UInt16);
        cl.SetPipeline(_pipeline);
        cl.SetGraphicsResourceSet(0, _mainResourceSet);

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        // Render command lists
        var vtxOffset = 0;
        var idxOffset = 0;
        for (var n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdListsRange[n];
            for (var cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                var pcmd = cmdList.CmdBuffer[cmdI];
                if(pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }

                if(pcmd.TextureId != IntPtr.Zero)
                {
                    if(pcmd.TextureId == _fontAtlasId)
                    {
                        cl.SetGraphicsResourceSet(1, _fontTextureResourceSet);
                    }
                    else
                    {
                        cl.SetGraphicsResourceSet(1, GetImageResourceSet(pcmd.TextureId));
                    }
                }

                cl.SetScissorRect(
                    0,
                    (uint)pcmd.ClipRect.X,
                    (uint)pcmd.ClipRect.Y,
                    (uint)(pcmd.ClipRect.Z - pcmd.ClipRect.X),
                    (uint)(pcmd.ClipRect.W - pcmd.ClipRect.Y));

                cl.DrawIndexed(pcmd.ElemCount, 1, pcmd.IdxOffset + (uint)idxOffset, (int)pcmd.VtxOffset + vtxOffset, 0);
            }

            vtxOffset += cmdList.VtxBuffer.Size;
            idxOffset += cmdList.IdxBuffer.Size;
        }
    }

    /// <summary>
    ///     Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        _vertexBuffer.Dispose();
        _indexBuffer.Dispose();
        _projMatrixBuffer.Dispose();
        _fontTexture.Dispose();
        _fontTextureView.Dispose();
        _vertexShader.Dispose();
        _fragmentShader.Dispose();
        _layout.Dispose();
        _textureLayout.Dispose();
        _pipeline.Dispose();
        _mainResourceSet.Dispose();

        foreach (var resource in _ownedResources)
        {
            resource.Dispose();
        }
    }

    private struct ResourceSetInfo
    {
        public readonly IntPtr ImGuiBinding;
        public readonly ResourceSet ResourceSet;

        public ResourceSetInfo(IntPtr imGuiBinding, ResourceSet resourceSet)
        {
            ImGuiBinding = imGuiBinding;
            ResourceSet = resourceSet;
        }
    }
}