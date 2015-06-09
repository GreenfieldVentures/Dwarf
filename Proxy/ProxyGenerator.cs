using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Evergreen.Dwarf.DataAccess;
using Evergreen.Dwarf.Extensions;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.Proxy
{    
    /// <summary>
    /// Static helper class for generating proxy classes
    /// </summary>
    internal static class ProxyGenerator
    {
        #region Variables

        private const MethodAttributes attribs = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual;

        #endregion Variables

        #region Methods

        #region Create

        /// <summary>
        /// Generates a proxy class from the supplied IDwarf type
        /// </summary>
        internal static void Create(Type baseType, IEnumerable<ExpressionProperty> overriddenProperties)
        {
            var assemblyName = new AssemblyName { Name = baseType.Assembly.GetName().Name + "Proxies" };

            var typeBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run)
                                .DefineDynamicModule(assemblyName.Name + ".dll")
                                .DefineType(baseType.Name, TypeAttributes.Public, baseType, new[] {typeof (IProxy)});

            var fields = new List<FieldBuilder>();

            foreach (var pi in overriddenProperties)
            {
                var propertyName = pi.Name;
                propertyName = propertyName.Substring(0, 1).ToUpper() + propertyName.Substring(1, propertyName.Length - 1);

                var fieldName = propertyName.Substring(0, 1).ToLower() + propertyName.Substring(1, propertyName.Length - 1);

                var returntype = pi.PropertyType;
                var backingField = typeBuilder.DefineField(fieldName, returntype, FieldAttributes.Private);
                var accessedField = fields.AddX(typeBuilder.DefineField("accessed" + propertyName, typeof(bool), FieldAttributes.Private));

                var getMethodIL = typeBuilder.DefineMethod("get_" + propertyName, attribs, returntype, Type.EmptyTypes).GetILGenerator();
                getMethodIL.Emit(OpCodes.Ldarg_0);
                getMethodIL.Emit(OpCodes.Ldstr, pi.Name);
                getMethodIL.Emit(OpCodes.Ldarg_0);
                getMethodIL.Emit(OpCodes.Ldflda, backingField);
                getMethodIL.Emit(OpCodes.Ldarg_0);
                getMethodIL.Emit(OpCodes.Ldflda, accessedField);
                getMethodIL.Emit(OpCodes.Call, baseType.GetMethod("GetPropertyInfo", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(new[] { pi.PropertyType }));
                getMethodIL.Emit(OpCodes.Ret);

                var setMethodIL = typeBuilder.DefineMethod("set_" + propertyName, attribs, null, new[] { returntype }).GetILGenerator();
                setMethodIL.Emit(OpCodes.Ldarg_0);
                setMethodIL.Emit(OpCodes.Ldarg_1);
                setMethodIL.Emit(OpCodes.Stfld, backingField);
                setMethodIL.Emit(OpCodes.Ldarg_0);
                setMethodIL.Emit(OpCodes.Ldstr, pi.Name);
                setMethodIL.Emit(OpCodes.Ldarg_0);
                setMethodIL.Emit(OpCodes.Ldflda, accessedField);
                setMethodIL.Emit(OpCodes.Call, baseType.GetMethod("SetProperty", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(new[] { pi.PropertyType }));
                setMethodIL.Emit(OpCodes.Ret);
            }

            var resetMethodIL = typeBuilder.DefineMethod("ResetFKProperties", MethodAttributes.FamORAssem | MethodAttributes.SpecialName | MethodAttributes.HideBySig | MethodAttributes.Virtual).GetILGenerator();
            resetMethodIL.Emit(OpCodes.Nop);
            resetMethodIL.Emit(OpCodes.Ldarg_0);
            resetMethodIL.Emit(OpCodes.Call, baseType.GetMethod("ResetFKProperties", BindingFlags.Instance | BindingFlags.NonPublic));
            resetMethodIL.Emit(OpCodes.Nop);

            foreach (var accessedField in fields)
            {
                resetMethodIL.Emit(OpCodes.Ldarg_0);
                resetMethodIL.Emit(OpCodes.Ldc_I4_0);
                resetMethodIL.Emit(OpCodes.Stfld, accessedField);
            }
            resetMethodIL.Emit(OpCodes.Ret);

            var proxyType = typeBuilder.CreateType();

            var ctor = proxyType.GetConstructor(new Type[0]);
            var lamda = Expression.Lambda<Func<object>>(Expression.New(ctor));
            Cfg.ProxyTypeConstructors[baseType] = lamda.Compile();
        }

        #endregion Create

        #endregion Methods
    }
}
