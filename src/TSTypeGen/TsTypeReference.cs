using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace TSTypeGen
{
    public abstract class TsTypeReference
    {
        public bool IsOptional { get; protected set; }
        public abstract string GetSource();

        public static TsTypeReference Simple(string type, bool isOptional = false)
        {
            return new SimpleTsTypeReference(type, isOptional);
        }

        public static TsTypeReference Dictionary(TsTypeReference keyType, TsTypeReference valueType)
        {
            return new DictionaryTsTypeReference(keyType, valueType);
        }

        public static TsTypeReference Array(TsTypeReference elementTypeReference)
        {
            return new ArrayTsTypeReference(elementTypeReference);
        }

        public static TsTypeReference Generic(TsTypeReference genericType, IEnumerable<TsTypeReference> arguments)
        {
            return new GenericTsTypeReference(genericType, arguments);
        }

        public abstract bool Equals(TsTypeDefinition tsTypeDefinition);

        private class SimpleTsTypeReference : TsTypeReference
        {
            private readonly string _type;

            public SimpleTsTypeReference(string type, bool isOptional)
            {
                _type = type;
                IsOptional = isOptional;
            }

            public override string GetSource()
            {
                return _type;
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return _type == tsTypeDefinition.Name;
            }
        }

        private class GenericTsTypeReference : TsTypeReference
        {
            private readonly TsTypeReference _genericType;
            private readonly ImmutableArray<TsTypeReference> _arguments;

            public GenericTsTypeReference(TsTypeReference genericType, IEnumerable<TsTypeReference> arguments)
            {
                _genericType = genericType;
                _arguments = ImmutableArray.CreateRange(arguments);
            }

            public override string GetSource()
            {
                var typeArguments = _arguments.Select(a =>
                {
                    var typeDef = a.GetSource();
                    if (a.IsOptional)
                        typeDef += " | undefined";

                    return typeDef;
                });
                return _genericType.GetSource() + "<" + string.Join(", ", typeArguments) + ">";
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return _genericType.Equals(tsTypeDefinition);
            }
        }

        private class ArrayTsTypeReference : TsTypeReference
        {
            private readonly TsTypeReference _itemType;

            public ArrayTsTypeReference(TsTypeReference itemType) {
                _itemType = itemType;
            }

            public override string GetSource()
            {
                if (_itemType.IsOptional)
                    return $"({_itemType.GetSource()} | undefined)[]";

                return $"{_itemType.GetSource()}[]";
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return false;
            }
        }

        private class DictionaryTsTypeReference : TsTypeReference
        {
            private readonly TsTypeReference _keyType;
            private readonly TsTypeReference _valueType;

            public DictionaryTsTypeReference(TsTypeReference keyType, TsTypeReference valueType) {
                _keyType = keyType;
                _valueType = valueType;
            }

            public override string GetSource()
            {
                var value = _valueType.GetSource();
                if (_valueType.IsOptional)
                    value += " | undefined";

                return $"{{[item: {_keyType.GetSource()}]: {value}}}";
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return false;
            }
        }
    }
}
