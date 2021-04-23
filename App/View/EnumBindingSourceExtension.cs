using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Markup;

namespace ElectricSketch.View
{
    // Usage ItemsSource="{Binding Source={local:EnumBindingSource {x:Type local:SomeEnum}}}"
    [MarkupExtensionReturnType(typeof(IEnumerable<Enum>))]
    public class EnumBindingSourceExtension : MarkupExtension
    {
        [ConstructorArgument("enumType")]
        public Type EnumType
        {
            get { return enumType; }
            set
            {
                if (value != enumType)
                {
                    if (value != null && !(Nullable.GetUnderlyingType(value) ?? value).IsEnum)
                        throw new ArgumentException($"{value.FullName} is not an Enum or Nullable<Enum>");

                    enumType = value;
                }
            }
        }
        Type enumType;

        public EnumBindingSourceExtension() { }

        public EnumBindingSourceExtension(Type enumType)
        {
            EnumType = enumType;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (enumType == null)
                throw new InvalidOperationException("Enum type not set.");

            var actualEnumType = Nullable.GetUnderlyingType(enumType) ?? enumType;
            var values = Enum.GetValues(actualEnumType);

            if (actualEnumType == enumType)
                return values;

            var nullableValues = Array.CreateInstance(actualEnumType, values.Length + 1); // 0 is null
            values.CopyTo(nullableValues, 1);
            return nullableValues;
        }
    }
}
