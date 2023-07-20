using ConcurrentCollections;
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
            ImGui.Text($"Frame time: {TimeSpan.FromSeconds(1).TotalMilliseconds / ImGui.GetIO().Framerate:F2}ms/frame ({ImGui.GetIO().Framerate:F1} FPS)");
            ImGui.NewLine();

            DrawModelTranslation();
            ImGui.NewLine();

            DrawModelRotation();
            ImGui.NewLine();

            ImGui.Checkbox("Move objects", ref _withMovements);
            ImGui.SameLine();
            ImGui.Checkbox("Create in random position", ref _createInRandomPosition);
            ImGui.NewLine();

            ClearElementsRequested = ImGui.Button("Clear elements");
            ImGui.SameLine();
            GenerateManyElementsRequested = ImGui.Button("Generate random");
            ImGui.NewLine();

            ImGui.Text("Info:");
            foreach (var i in _info)
            {
                ImGui.Text(i);
            }
            _info.Clear();
        }
        ImGui.End();
    }

    private void DrawModelTranslation()
    {
        ImGui.Text("Translation");
        ImGui.SliderFloat("X mt", ref XModelTranslation.Value, XModelTranslation.Min, XModelTranslation.Max);
        ImGui.SliderFloat("Y mt", ref YModelTranslation.Value, YModelTranslation.Min, YModelTranslation.Max);
        ImGui.SliderFloat("Z mt", ref ZModelTranslation.Value, ZModelTranslation.Min, ZModelTranslation.Max);
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
    }
}