using G = Godot;

namespace Mirage.Godot.Scripts.Serialization
{
    public static class GodotTypesExtensions
    {
        public static void WriteVector2(this NetworkWriter writer, G.Vector2 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
        }

        public static void WriteVector3(this NetworkWriter writer, G.Vector3 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
            writer.WriteSingle(value.Z);
        }

        public static void WriteVector4(this NetworkWriter writer, G.Vector4 value)
        {
            writer.WriteSingle(value.X);
            writer.WriteSingle(value.Y);
            writer.WriteSingle(value.Z);
            writer.WriteSingle(value.W);
        }

        public static void WriteColor(this NetworkWriter writer, G.Color value)
        {
            writer.WriteSingle(value.R);
            writer.WriteSingle(value.G);
            writer.WriteSingle(value.B);
            writer.WriteSingle(value.A);
        }

        public static void WritePlane(this NetworkWriter writer, G.Plane value)
        {
            writer.WriteVector3(value.Normal);
            writer.WriteSingle(value.D);
        }



        public static G.Vector2 ReadVector2(this NetworkReader reader) => new G.Vector2(reader.ReadSingle(), reader.ReadSingle());
        public static G.Vector3 ReadVector3(this NetworkReader reader) => new G.Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static G.Vector4 ReadVector4(this NetworkReader reader) => new G.Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static G.Color ReadColor(this NetworkReader reader) => new G.Color(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        public static G.Plane ReadPlane(this NetworkReader reader) => new G.Plane(reader.ReadVector3(), reader.ReadSingle());
    }

    public static class GodotCollectionExtensions
    {
        [WeaverSerializeCollection]
        public static void WriteGodotArray<[G.MustBeVariant] T>(this NetworkWriter writer, G.Collections.Array<T> array)
        {
            CollectionExtensions.WriteCountPlusOne(writer, array?.Count);

            if (array is null)
                return;

            var length = array.Count;
            for (var i = 0; i < length; i++)
                writer.Write(array[i]);
        }

        [WeaverSerializeCollection]
        public static void WriteGodotDictionary<[G.MustBeVariant] TKey, [G.MustBeVariant] TValue>(this NetworkWriter writer, G.Collections.Dictionary<TKey, TValue> dictionary)
        {
            CollectionExtensions.WriteCountPlusOne(writer, dictionary?.Count);

            if (dictionary is null)
                return;

            foreach (var kvp in dictionary)
            {
                writer.Write(kvp.Key);
                writer.Write(kvp.Value);
            }
        }


        [WeaverSerializeCollection]
        public static G.Collections.Array<T> ReadGodotArray<[G.MustBeVariant] T>(this NetworkReader reader)
        {
            var hasValue = CollectionExtensions.ReadCountPlusOne(reader, out var length);
            if (!hasValue)
                return null;

            CollectionExtensions.ValidateSize(reader, length);

            var result = new G.Collections.Array<T>();
            result.Resize(length);
            for (var i = 0; i < length; i++)
            {
                result[i] = reader.Read<T>();
            }
            return result;
        }

        [WeaverSerializeCollection]
        public static G.Collections.Dictionary<TKey, TValue> ReadGodotDictionary<[G.MustBeVariant] TKey, [G.MustBeVariant] TValue>(this NetworkReader reader)
        {
            var hasValue = CollectionExtensions.ReadCountPlusOne(reader, out var length);
            if (!hasValue)
                return null;

            CollectionExtensions.ValidateSize(reader, length);

            var result = new G.Collections.Dictionary<TKey, TValue>();
            for (var i = 0; i < length; i++)
            {
                var key = reader.Read<TKey>();
                var value = reader.Read<TValue>();
                result[key] = value;
            }
            return result;
        }
    }
}