﻿using PixiEditor.DrawingApi.Core;

namespace PixiEditor.Models.Serialization.Factories;

public class TextureSerializationFactory : SerializationFactory<byte[], Texture>
{
    private SurfaceSerializationFactory SurfaceFactory { get; } = new SurfaceSerializationFactory();
    public override byte[] Serialize(Texture original)
    {
        Surface surface = new Surface(original.Size);
        surface.DrawingSurface.Canvas.DrawSurface(original.DrawingSurface, 0, 0);
        return SurfaceFactory.Serialize(surface);
    }

    public override bool TryDeserialize(object serialized, out Texture original)
    {
        if (serialized is byte[] imgBytes)
        {
            if (SurfaceFactory.TryDeserialize(imgBytes, out Surface surface))
            {
                original = new Texture(surface.Size);
                original.DrawingSurface.Canvas.DrawSurface(surface.DrawingSurface, 0, 0);
                return true;
            }
        }

        original = null;
        return false; 
    }


    public override string DeserializationId { get; } = "PixiEditor.Texture";
}
