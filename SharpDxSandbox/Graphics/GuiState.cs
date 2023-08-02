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

    public bool ClearElementsRequested { get; protected set; }

    public bool GenerateManyElementsRequested { get; protected set; }

    public (float X, float Y, float Z) ModelRotation => (XModelRotation.Value, YModelRotation.Value, ZModelRotation.Value);

    public (float X, float Y, float Z) ModelTranslation => (XModelTranslation.Value, YModelTranslation.Value, ZModelTranslation.Value);
    
    public (float X, float Y, float Z) LightSourcePosition => (XLightPosition.Value, YLightPosition.Value, ZLightPosition.Value);

    public SimpleObjectsRequest CreateSimpleObjectRequest { get; protected set; } = new(false, false, false, false);

    public SkinnedObjectRequest CreateSkinnedObjectRequest { get; protected set; } = new(false, false, false);

    public ShadedObjectRequest CreateShadedObjectRequest { get; protected set; } = new(false);

    public bool CreateInRandomPosition => _createInRandomPosition;

    public bool WithMovements => _withMovements;

    protected void InitState()
    {
        XModelTranslation = SliderData.Create(0, -20, 20);
        YModelTranslation = SliderData.Create(0, -20, 20);
        ZModelTranslation = SliderData.Create(0, -20, 20);

        XLightPosition = SliderData.Create(0, -20, 20);
        YLightPosition = SliderData.Create(0, -20, 20);
        ZLightPosition = SliderData.Create(0, -20, 20);

        InitRotations();
    }

    protected void InitRotations()
    {
        const float circle = 2f * (float)Math.PI;
        XModelRotation = SliderData.Create(0, -circle, circle);
        YModelRotation = SliderData.Create(0, -circle, circle);
        ZModelRotation = SliderData.Create(0, -circle, circle);
    }

    protected sealed record SliderData(float Min, float Max)
    {
        private float _value;

        public static SliderData Create(float initial, float min, float max) => new(min, max) { Value = initial };

        public ref float Value => ref _value;
    }

    public sealed record SimpleObjectsRequest(bool SimpleCube, bool ColoredCube, bool ColoredFromModelFile, bool ColoredSphere);

    public sealed record SkinnedObjectRequest(bool Plane, bool SkinnedCube, bool SkinnedCubeFromModelFile);

    public sealed record ShadedObjectRequest(bool ShadedSkinnedCube);
}