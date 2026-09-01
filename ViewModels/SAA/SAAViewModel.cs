using System.Runtime.CompilerServices;
using TaleWorlds.Library;

namespace StartAsAnyone
{
    /// <summary>
    /// Base for the redesigned Start As Anyone view models.
    ///
    /// Gauntlet raises a separate change event per property type, so one generic setter
    /// would push bools and ints down the object channel where the widget never listens.
    /// Hence a setter per channel. Each takes the property name from the caller, so a
    /// property cannot notify under a neighbour's name.
    /// </summary>
    public abstract class SAAViewModel : ViewModel
    {
        protected bool SetBool(ref bool field, bool value, [CallerMemberName] string propertyName = "")
        {
            if (field == value)
            {
                return false;
            }
            field = value;
            OnPropertyChangedWithValue(value, propertyName);
            return true;
        }

        protected bool SetInt(ref int field, int value, [CallerMemberName] string propertyName = "")
        {
            if (field == value)
            {
                return false;
            }
            field = value;
            OnPropertyChangedWithValue(value, propertyName);
            return true;
        }

        protected bool SetString(ref string field, string value, [CallerMemberName] string propertyName = "")
        {
            value ??= string.Empty;
            if (field == value)
            {
                return false;
            }
            field = value;
            OnPropertyChangedWithValue<string>(value, propertyName);
            return true;
        }

        protected bool SetRef<T>(ref T? field, T? value, [CallerMemberName] string propertyName = "") where T : class
        {
            if (ReferenceEquals(field, value))
            {
                return false;
            }
            field = value;
            OnPropertyChangedWithValue<T?>(value, propertyName);
            return true;
        }
    }
}
