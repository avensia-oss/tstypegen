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
        public abstract string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases);
        protected abstract void GatherImportedModules(List<ImportedModule> list);

        public ImmutableArray<ImportedModule> GetImportStatements()
        {
            var result = new List<ImportedModule>();
            GatherImportedModules(result);
            return ImmutableArray.CreateRange(result);
        }

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

        public static TsTypeReference DefaultImportedType(string name, string sourceFile, bool isOptional = false)
        {
            return new ImportedTsTypeReference(name, sourceFile, true, isOptional);
        }

        public static TsTypeReference NameImportedType(string name, string sourceFile, bool isOptional = false)
        {
            return new ImportedTsTypeReference(name, sourceFile, false, isOptional);
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

            public override string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases)
            {
                return _type;
            }

            protected override void GatherImportedModules(List<ImportedModule> list)
            {
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

            public GenericTsTypeReference(TsTypeReference genericType, IEnumerable<TsTypeReference> arguments) {
                _genericType = genericType;
                _arguments = ImmutableArray.CreateRange(arguments);
            }

            public override string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases)
            {
                var typeArguments = _arguments.Select(a =>
                {
                    var typeDef = a.GetSource(isNamespaceFile, useOptionalForNullables, importAliases);
                    if (a.IsOptional && useOptionalForNullables)
                        typeDef += " | undefined";

                    return typeDef;
                });
                return _genericType.GetSource(isNamespaceFile, useOptionalForNullables, importAliases) + "<" + string.Join(", ", typeArguments) + ">";
            }

            protected override void GatherImportedModules(List<ImportedModule> list)
            {
                _genericType.GatherImportedModules(list);
                foreach (var a in _arguments)
                {
                    a.GatherImportedModules(list);
                }
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return _genericType.Equals(tsTypeDefinition);
            }
        }

        private class ImportedTsTypeReference : TsTypeReference
        {
            private readonly string _name;
            private readonly string _sourceFile;
            private readonly bool _isDefaultImport;

            public ImportedTsTypeReference(string name, string sourceFile, bool isDefaultImport, bool isOptional)
            {
                _name = name;
                _sourceFile = sourceFile;
                _isDefaultImport = isDefaultImport;
                IsOptional = isOptional;
            }

            public override string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases)
            {
                var key = new ImportedType(_sourceFile, _isDefaultImport ? null : _name);
                return isNamespaceFile ? "__ImportedModules." + importAliases[key] : importAliases[key];
            }

            protected override void GatherImportedModules(List<ImportedModule> list)
            {
                list.Add(_isDefaultImport ? ImportedModule.DefaultImport(_name, _sourceFile) : ImportedModule.NamedImport(_name, _sourceFile, _name));
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return _name == tsTypeDefinition.Name;
            }
        }

        private class ArrayTsTypeReference : TsTypeReference
        {
            private readonly TsTypeReference _itemType;

            public ArrayTsTypeReference(TsTypeReference itemType) {
                _itemType = itemType;
            }

            public override string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases)
            {
                if (_itemType.IsOptional && useOptionalForNullables)
                    return $"({_itemType.GetSource(isNamespaceFile, useOptionalForNullables, importAliases)} | undefined)[]";

                return $"{_itemType.GetSource(isNamespaceFile, useOptionalForNullables, importAliases)}[]";
            }

            protected override void GatherImportedModules(List<ImportedModule> list)
            {
                _itemType.GatherImportedModules(list);
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

            public override string GetSource(bool isNamespaceFile, bool useOptionalForNullables, IDictionary<ImportedType, string> importAliases)
            {
                var value = _valueType.GetSource(isNamespaceFile, useOptionalForNullables, importAliases);
                if (_valueType.IsOptional && useOptionalForNullables)
                    value += " | undefined";

                return $"{{[item: {_keyType.GetSource(isNamespaceFile, useOptionalForNullables, importAliases)}]: {value}}}";
            }

            protected override void GatherImportedModules(List<ImportedModule> list)
            {
                _keyType.GatherImportedModules(list);
                _valueType.GatherImportedModules(list);
            }

            public override bool Equals(TsTypeDefinition tsTypeDefinition)
            {
                return false;
            }
        }
    }
}
