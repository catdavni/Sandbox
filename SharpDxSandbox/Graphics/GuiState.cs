namespace SharpDxSandbox.Graphics;

internal abstract class GuiState
{
    protected SliderData XModelTranslation;
    protected SliderData YModelTranslation;
    protected SliderData ZModelTranslation;

    protected SliderData XModelRotation;
    protected SliderData YModelRotation;
    protected SliderData ZModelRotation;
    
    protected bool _createInRandomPosition;
    protected bool _withMovements;

    public bool ClearElementsRequested { get; protected set; }

    public bool GenerateManyElementsRequested { get; protected set; }

    public (float X, float Y, float Z) ModelRotation => (XModelRotation.Value, YModelRotation.Value, ZModelRotation.Value);

    public (float X, float Y, float Z) ModelTranslation => (XModelTranslation.Value, YModelTranslation.Value, ZModelTranslation.Value);

    public bool CreateInRandomPosition => _createInRandomPosition;

    public bool WithMovements => _withMovements;

    protected void InitState()
    {
        XModelTranslation = SliderData.Create(0, -50, 50);
        YModelTranslation = SliderData.Create(0, -50, 50);
        ZModelTranslation = SliderData.Create(0, -50, 50);

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
}