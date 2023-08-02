using ConcurrentCollections;
using ImGuiNET;
using SharpDX.Direct3D11;
using SharpDxSandbox.Infrastructure;
using Vanara.PInvoke;
using ImGui = ImGuiNET.ImGui;

namespace SharpDxSandbox.Graphics;

internal sealed class GuiManager : GuiState, IDisposable
{
    private readonly float _windowScaleInPercents;
    private readonly ConcurrentHashSet<string> _info;

    public GuiManager(Device device, Window window)
    {
        _info = new();
        InitState();

        unsafe
        {
            ImGui.CreateContext();
            ImGui.ImGui_ImplWin32_Init(window.Handle);
            ImGui.ImGui_ImplDX11_Init(device.NativePointer.ToPointer(), device.ImmediateContext.NativePointer.ToPointer());

            _windowScaleInPercents = User32.GetDpiForWindow(window.Handle) / 100f;

            window.RegisterWindowProcHandler("ImGui",
                (hwnd, msg, wparam, lparam)
                    => ImGui.ImGui_ImplWin32_WndProcHandler(hwnd.DangerousGetHandle().ToPointer(), msg, wparam, lparam));
        }
    }

    public void PrintInfo(string info)
    {
        _info.Add(info);
    }

    public void Draw()
    {
        ImGui.ImGui_ImplWin32_NewFrame();
        ImGui.ImGui_ImplDX11_NewFrame();
        ImGui.NewFrame();

        ImGui.GetFont().Scale = _windowScaleInPercents;

        CreateLayout();

        ImGui.Render();
        ImGui.ImGui_ImplDX11_RenderDrawData(ImGui.GetDrawData());
    }

    public void Dispose()
    {
        ImGui.ImGui_ImplDX11_Shutdown();
        ImGui.ImGui_ImplWin32_Shutdown();
        ImGui.DestroyContext();
    }

    private void CreateLayout()
    {
        if (ImGui.Begin("Controls"))
        {
            DrawFpsInfo();

            DrawModelTranslation();

            DrawModelRotation();

            DrawLightSourcePosition();

            DrawObjectManagement();

            DrawObjectFactory();

            DrawInfoLog();
        }
        ImGui.End();
    }

    private static void DrawFpsInfo()
    {
        ImGui.Text($"Frame time: {TimeSpan.FromSeconds(1).TotalMilliseconds / ImGui.GetIO().Framerate:F2}ms/frame ({ImGui.GetIO().Framerate:F1} FPS)");
        ImGui.NewLine();
    }

    private void DrawModelTranslation()
    {
        ImGui.Text("Translation");
        ImGui.SliderFloat("X mt", ref XModelTranslation.Value, XModelTranslation.Min, XModelTranslation.Max);
        ImGui.SliderFloat("Y mt", ref YModelTranslation.Value, YModelTranslation.Min, YModelTranslation.Max);
        ImGui.SliderFloat("Z mt", ref ZModelTranslation.Value, ZModelTranslation.Min, ZModelTranslation.Max);
        ImGui.NewLine();
    }

    private void DrawObjectManagement()
    {
        ImGui.Checkbox("Move objects", ref _withMovements);
        ImGui.SameLine();
        ImGui.Checkbox("Create in random position", ref _createInRandomPosition);
        ImGui.NewLine();

        ClearElementsRequested = ImGui.Button("Clear elements");
        ImGui.SameLine();
        GenerateManyElementsRequested = ImGui.Button("Generate random");
        ImGui.NewLine();
    }

    private void DrawModelRotation()
    {
        ImGui.Text("Rotation");
        ImGui.SliderFloat("X rad", ref XModelRotation.Value, XModelRotation.Min, XModelRotation.Max);
        ImGui.SliderFloat("Y rad", ref YModelRotation.Value, YModelRotation.Min, YModelRotation.Max);
        ImGui.SliderFloat("Z rad", ref ZModelRotation.Value, ZModelRotation.Min, ZModelRotation.Max);
        if (ImGui.Button("Reset rotation"))
        {
            InitRotations();
        }
        ImGui.NewLine();
    }
    
    private void DrawLightSourcePosition()
    {
        ImGui.Text("LightSource");
        ImGui.SliderFloat("X l", ref XLightPosition.Value, XLightPosition.Min, XLightPosition.Max);
        ImGui.SliderFloat("Y l", ref YLightPosition.Value, YLightPosition.Min, YLightPosition.Max);
        ImGui.SliderFloat("Z l", ref ZLightPosition.Value, ZLightPosition.Min, ZLightPosition.Max);
        ImGui.NewLine();
    }

    private void DrawInfoLog()
    {
        ImGui.Text("Info:");
        foreach (var i in _info)
        {
            ImGui.Text(i);
        }
        _info.Clear();
    }

    private void DrawObjectFactory()
    {
        if (ImGui.BeginTabBar("Create", ImGuiTabBarFlags.None))
        {
            if (ImGui.BeginTabItem("Simple"))
            {
                CreateSimpleObjectRequest = CreateSimpleObjectRequest with { SimpleCube = ImGui.Button("Line cube") };
                CreateSimpleObjectRequest = CreateSimpleObjectRequest with { ColoredCube = ImGui.Button("Colored cube") };
                CreateSimpleObjectRequest = CreateSimpleObjectRequest with { ColoredFromModelFile = ImGui.Button("Colored cube from obj file") };
                CreateSimpleObjectRequest = CreateSimpleObjectRequest with { ColoredSphere = ImGui.Button("Colored sphere") };
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Skinned"))
            {
                CreateSkinnedObjectRequest = CreateSkinnedObjectRequest with { Plane = ImGui.Button("Plane") };
                CreateSkinnedObjectRequest = CreateSkinnedObjectRequest with { SkinnedCube = ImGui.Button("Skinned cube") };
                CreateSkinnedObjectRequest = CreateSkinnedObjectRequest with { SkinnedCubeFromModelFile = ImGui.Button("Skinned cube from obj file") };
                ImGui.EndTabItem();
            }
            
            if (ImGui.BeginTabItem("Shaded"))
            {
                CreateShadedObjectRequest = CreateShadedObjectRequest with { ShadedSkinnedCube = ImGui.Button("Skinned shaded cube") };
                ImGui.EndTabItem();
            }
            
            ImGui.EndTabBar();
        }
        
        ImGui.NewLine();
    }
}