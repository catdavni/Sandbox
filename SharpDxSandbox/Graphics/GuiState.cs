using SharpDX;

namespace SharpDxSandbox.Graphics;

internal abstract class GuiState
{
    protected SliderData XModelTranslation;
    protected SliderData YModelTranslation;
    protected SliderData ZModelTranslation;

    protected SliderData XModelRotation;
    protected SliderData YModelRotation;
    protected SliderData ZModelRotation;

    protected SliderData XLightPosition;
    protected SliderData YLightPosition;
    protected SliderData ZLightPosition;

    protected bool _createInRandomPosition;
    protected bool _withMovements;

    protected System.Numerics.Vector4 _materialColor;
    protected SliderData MaterialAmbientSlider;
    protected SliderData MaterialDiffuseIntensitySlider;
    protected SliderData MaterialSpecularIntensitySlider;
    protected SliderData MaterialSpecularPowerSlider;
    protected SliderData MaterialAttenuationConstantSlider;
    protected SliderData MaterialAttenuationLinearSlider;
    protected SliderData MaterialAttenuationQuadricSlider;

    public bool ClearElementsRequested { get; protected set; }

    public bool GenerateManyElementsRequested { get; protected set; }

    public (float X, float Y, float Z) ModelRotation => (XModelRotation.Value, YModelRotation.Value, ZModelRotation.Value);

    public (float X, float Y, float Z) ModelTranslation => (XModelTranslation.Value, YModelTranslation.Value, ZModelTranslation.Value);

    public (float X, float Y, float Z) LightSourcePosition => (XLightPosition.Value, YLightPosition.Value, ZLightPosition.Value);

    public Vector4 MaterialColor => new(_materialColor.X, _materialColor.Y, _materialColor.Z, _materialColor.W);

    public (float Ambient, float DiffuseIntensity, float SpecularIntensity, float SpecularPower) LightTraits
        => (
            MaterialAmbientSlider.Value,
            MaterialDiffuseIntensitySlider.Value,
            MaterialSpecularIntensitySlider.Value,
            MaterialSpecularPowerSlider.Value);

    public (float Constant, float Linear, float Quardic) MaterialAttenuation
        => (MaterialAttenuationConstantSlider.Value, MaterialAttenuationLinearSlider.Value, MaterialAttenuationQuadricSlider.Value);

    public SimpleObjectsRequest CreateSimpleObjectRequest { get; protected set; }

    public SkinnedObjectRequest CreateSkinnedObjectRequest { get; protected set; }

    public ShadedObjectRequest CreateShadedObjectRequest { get; protected set; }

    public bool CreateInRandomPosition => _createInRandomPosition;

    public bool WithMovements => _withMovements;

    protected void InitState()
    {
        XLightPosition = SliderData.Create(0, -20, 20);
        YLightPosition = SliderData.Create(0, -20, 20);
        ZLightPosition = SliderData.Create(0, -4, 100);

        InitTranslations();
        InitRotations();
        InitMaterial();
    }

    protected void InitTranslations()
    {
        XModelTranslation = SliderData.Create(0, -20, 20);
        YModelTranslation = SliderData.Create(0, -20, 20);
        ZModelTranslation = SliderData.Create(0, -20, 150);
    }

    protected void InitRotations()
    {
        const float circle = 2f * (float)Math.PI;
        XModelRotation = SliderData.Create(0, -circle, circle);
        YModelRotation = SliderData.Create(0, -circle, circle);
        ZModelRotation = SliderData.Create(0, -circle, circle);
    }

    protected void InitMaterial()
    {
        _materialColor = System.Numerics.Vector4.Zero;
        MaterialAmbientSlider = SliderData.Create(0, -1, 1);
        MaterialDiffuseIntensitySlider = SliderData.Create(0, -3, 3);
        MaterialSpecularIntensitySlider = SliderData.Create(0, 0, 10);
        MaterialSpecularPowerSlider = SliderData.Create(0, 0, 200);
        MaterialAttenuationConstantSlider = SliderData.Create(0, -1f, 1);
        MaterialAttenuationLinearSlider = SliderData.Create(0, 0f, 0.01f);
        MaterialAttenuationQuadricSlider = SliderData.Create(0, 0f, 0.001f);
    }

    protected sealed record SliderData(float Min, float Max)
    {
        private float _value;

        public static SliderData Create(float initial, float min, float max) => new(min, max) { Value = initial };

        public ref float Value => ref _value;
    }

    public record struct SimpleObjectsRequest(bool SimpleCube, bool ColoredCube, bool ColoredFromModelFile, bool ColoredSphere);

    public record struct SkinnedObjectRequest(bool Plane, bool SkinnedCube, bool SkinnedCubeFromModelFile);

    public record struct ShadedObjectRequest(
        bool GouraudShadedSkinnedCube,
        bool GouraudShadedSphere,
        bool GouraudSmoothShadedSphere,
        bool GouraudShadedSuzanne,
        bool PhongShadedSphere,
        bool PhongShadedCube,
        bool PhongShadedSuzanne,
        bool NanoSuit);
}