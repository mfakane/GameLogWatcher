using System;
using System.Text;

namespace GameLogWatcher
{
	static class EnumEx
	{
		public static TEnum ParseFlags<TEnum>(string value, bool ignoreCase)
			where TEnum : struct
		{
			var index = 0;

			return ParseBinary<TEnum>(value, ignoreCase, ref index);
		}

		static TEnum ParseBinary<TEnum>(string value, bool ignoreCase, ref int index)
			where TEnum : struct
		{
			var left = ParseUnary<TEnum>(value, ignoreCase, ref index);

			while (index < value.Length)
				if (char.IsWhiteSpace(value, index))
					index++;
				else
					switch (value[index])
					{
						case '&':
							{
								index++;

								var right = ParseUnary<TEnum>(value, ignoreCase, ref index);

								left = (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(left) & Convert.ToInt32(right));

								break;
							}
						case '|':
							{
								index++;

								var right = ParseUnary<TEnum>(value, ignoreCase, ref index);

								left = (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(left) | Convert.ToInt32(right));

								break;
							}
						case '^':
							{
								index++;

								var right = ParseUnary<TEnum>(value, ignoreCase, ref index);

								left = (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(left) ^ Convert.ToInt32(right));

								break;
							}
						default:
							return left;
					}

			return left;
		}

		static TEnum ParseUnary<TEnum>(string value, bool ignoreCase, ref int index)
			where TEnum : struct
		{
			while (index < value.Length && char.IsWhiteSpace(value[index]))
				index++;

			var startIndex = index;

			switch (value[index])
			{
				case '(':
					{
						index++;

						var operand = ParseBinary<TEnum>(value, ignoreCase, ref index);

						if (value[index] != ')')
							throw new FormatException($") expected on char {index}");

						index++;

						return operand;
					}
				case '~':
					{
						index++;

						var operand = ParseUnary<TEnum>(value, ignoreCase, ref index);

						return (TEnum)Enum.ToObject(typeof(TEnum), ~Convert.ToInt32(operand));
					}
			}

			var sb = new StringBuilder();

			for (; index < value.Length; index++)
			{
				var c = value[index];

				if (char.IsLetterOrDigit(c) || c == '_')
					sb.Append(c);
				else
					return (TEnum)Enum.Parse(typeof(TEnum), sb.ToString(), ignoreCase);
			}

			return (TEnum)Enum.Parse(typeof(TEnum), sb.ToString(), ignoreCase);
		}
	}
}
