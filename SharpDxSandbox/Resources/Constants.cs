﻿namespace SharpDxSandbox.Resources;

public static class Constants
{
    public static class Images
    {
        public const string CatSquare = "1i.jpg";
        public const string CatGrumpyVertical = "4i.jpg";
        public const string CatCuriousVertical = "9i.jpg";
        public const string CubeSides = "cube.png";
    }
    
    public static class Models
    {
        public const string Cube = "cube.obj";
        public const string Sphere = "sphere.obj";
        public const string Suzanne = "suzanne.obj";

        public static class NanoSuit
        {
            public const string FolderName = "NanoSuit";
            public const string ModelName = "nanosuit.obj";
        }
    }
    
    public static class Shaders
    {
        public const string Test = "test.hlsl";
        public const string WithColorsConstantBuffer = "WithColorsCb.hlsl";
        public const string WithTexCoordAndSampler = "WithTexCoordAndSampler.hlsl";
        public const string LightSource = "LightSource.hlsl";
        public const string GouraudShading = "GouraudShading.hlsl";
        public const string PhongShading = "PhongShading.hlsl";
        public const string PhongShadingTextureBased = "PhongShadingTextureBased.hlsl";
    }
}