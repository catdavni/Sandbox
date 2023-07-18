using SharpDX.Direct3D11;
using SharpDxSandbox.Infrastructure;
using ImGui = ImGuiNET.ImGui;

namespace SharpDxSandbox.Graphics;

internal sealed class GuiManager : IDisposable
{
    private readonly float _windowScaleInPercents;
    private SliderData _xTranslation;
    private SliderData _yTranslation;
    private SliderData _zTranslation;

    private SliderData _xRotation;
    private SliderData _yRotation;
    private SliderData _zRotation;

    private bool _randomizeMovements;

    public GuiManager(Device device, Window window)
    {
        InitState();

        unsafe
        {
            ImGui.CreateContext();
            ImGui.ImGui_ImplWin32_Init(window.Handle);
            ImGui.ImGui_ImplDX11_Init(device.NativePointer.ToPointer(), device.ImmediateContext.NativePointer.ToPointer());

            _windowScaleInPercents = Vanara.PInvoke.User32.GetDpiForWindow(window.Handle) / 100f;

            window.RegisterWindowProcHandler("ImGui",
                (hwnd, msg, wparam, lparam)
                    => ImGui.ImGui_ImplWin32_WndProcHandler(hwnd.DangerousGetHandle().ToPointer(), msg, wparam, lparam));
        }
    }

    public event EventHandler<EventArgs> ClearElementsRequested;

    public event EventHandler<EventArgs> GenerateManyElementsRequested; 

    public float XTranslation => _xTranslation.Value;

    public float YTranslation => _yTranslation.Value;

    public float ZTranslation => _zTranslation.Value;

    public float XRotation => _xRotation.Value;

    public float YRotation => _yRotation.Value;

    public float ZRotation => _zRotation.Value;

    public bool RandomizeMovements => _randomizeMovements;

    public void Draw()
    {
        ImGui.ImGui_ImplWin32_NewFrame();
        ImGui.ImGui_ImplDX11_NewFrame();
        ImGui.NewFrame();

        ImGui.GetFont().Scale = _windowScaleInPercents;

        CreateLayout();
        //ImGui.ShowDemoWindow();

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
            //ImGui.BeginGroup();
            ImGui.Text("Translation");
            ImGui.SliderFloat("X", ref _xTranslation.Value, _xTranslation.Min, _xTranslation.Max);
            ImGui.SliderFloat("Y", ref _yTranslation.Value, _yTranslation.Min, _yTranslation.Max);
            ImGui.SliderFloat("Z", ref _zTranslation.Value, _zTranslation.Min, _zTranslation.Max);
            //ImGui.EndGroup();

            ImGui.NewLine();

            //ImGui.BeginGroup();
            ImGui.Text("Rotation");
            ImGui.SliderFloat("X rad", ref _xRotation.Value, _xRotation.Min, _xRotation.Max);
            ImGui.SliderFloat("Y rad", ref _yRotation.Value, _yRotation.Min, _yRotation.Max);
            ImGui.SliderFloat("Z rad", ref _zRotation.Value, _zRotation.Min, _zRotation.Max);
            if (ImGui.Button("Reset rotation"))
            {
                InitRotations();
            }
            //ImGui.EndGroup();

            ImGui.NewLine();

            ImGui.Checkbox("Randomize movements", ref _randomizeMovements);

            ImGui.NewLine();
            
            if (ImGui.Button("Clear elements"))
            {
                ClearElementsRequested?.Invoke(this, EventArgs.Empty);
            }
            ImGui.SameLine();
            if (ImGui.Button("Generate random"))
            {
                GenerateManyElementsRequested?.Invoke(this, EventArgs.Empty);
            }
            ImGui.End();
        }
    }

    private void InitState()
    {
        _xTranslation = SliderData.Create(0, -50, 50);
        _yTranslation = SliderData.Create(0, -50, 50);
        _zTranslation = SliderData.Create(0, -50, 50);

        InitRotations();
    }

    private void InitRotations()
    {
        _xRotation = SliderData.Create(0, -1f, 1f);
        _yRotation = SliderData.Create(0, -1f, 1f);
        _zRotation = SliderData.Create(0, -1f, 1f);
    }

    private sealed record SliderData(float Min, float Max)
    {
        private float _value;

        public static SliderData Create(float initial, float min, float max) => new(min, max) { Value = initial };

        public ref float Value => ref _value;
    }
}