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

    public SimpleObjectsRequest CreateSimpleObjectRequest { get; protected set; } = new(false, false, false, false);

    public SkinnedObjectRequest CreateSkinnedObjectRequest { get; protected set; } = new(false, false, false);

    public ShadedObjectRequest CreateShadedObjectRequest { get; protected set; } = new(false, false, false, false, false, false, false);

    public bool CreateInRandomPosition => _createInRandomPosition;

    public bool WithMovements => _withMovements;

    protected void InitState()
    {
        XLightPosition = SliderData.Create(0, -20, 20);
        YLightPosition = SliderData.Create(0, -20, 20);
        ZLightPosition = SliderData.Create(0, -20, 20);

        InitTranslations();
        InitRotations();
        InitMaterial();
    }

    protected void InitTranslations()
    {
        XModelTranslation = SliderData.Create(0, -20, 20);
        YModelTranslation = SliderData.Create(0, -20, 20);
        ZModelTranslation = SliderData.Create(0, -20, 20);
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
        MaterialSpecularIntensitySlider = SliderData.Create(0, -3, 3);
        MaterialSpecularPowerSlider = SliderData.Create(0, -3, 3);
        MaterialAttenuationConstantSlider = SliderData.Create(0, -0.5f, 1);
        MaterialAttenuationLinearSlider = SliderData.Create(0, -0.5f, 1);
        MaterialAttenuationQuadricSlider = SliderData.Create(0, -0.5f, 1);
    }

    protected sealed record SliderData(float Min, float Max)
    {
        private float _value;

        public static SliderData Create(float initial, float min, float max) => new(min, max) { Value = initial };

        public ref float Value => ref _value;
    }

    public sealed record SimpleObjectsRequest(bool SimpleCube, bool ColoredCube, bool ColoredFromModelFile, bool ColoredSphere);

    public sealed record SkinnedObjectRequest(bool Plane, bool SkinnedCube, bool SkinnedCubeFromModelFile);

    public sealed record ShadedObjectRequest(bool GouraudShadedSkinnedCube, bool GouraudShadedSphere, bool GouraudSmoothShadedSphere, bool GouraudShadedSuzanne, bool PhongShadedSphere, bool PhongShadedCube, bool PhongShadedSuzanne);
}