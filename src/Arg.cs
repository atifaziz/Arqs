#region Copyright (c) 2019 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Arqs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    public interface IArg : IArgBinder
    {
        IArgInfo Info { get; }
        IArg WithInfo(IArgInfo value);
        IAccumulator CreateAccumulator();
    }

    public interface IArg<out T, TInfo> : IArg, IArgBinder<T> where TInfo : IArgInfo
    {
        new TInfo Info { get; }
        IArg<T, TInfo> WithInfo(TInfo value);
        new IAccumulator<T> CreateAccumulator();
    }

    sealed class Arg<T, TInfo> : IArg<T, TInfo> where TInfo : IArgInfo
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(TInfo info, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Info = info;
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public TInfo Info { get; }
        IArgInfo IArg.Info => Info;

        public Arg<T, TInfo> WithInfo(TInfo value) =>
            new Arg<T, TInfo>(value, _accumulatorFactory, _binder);

        IArg<T, TInfo> IArg<T, TInfo>.WithInfo(TInfo value) => WithInfo(value);
        IArg IArg.WithInfo(IArgInfo value) => WithInfo((TInfo)value);

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();

        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        object IArgBinder.Bind(Func<IAccumulator> source) => Bind(source);

        public T Bind(Func<IAccumulator> source) =>
            _binder((IAccumulator<T>)source());

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public static class Arg
    {
        public static bool IsFlag(this IArg arg) =>
            arg.Info is FlagArgInfo;

        public static bool IsNegtableFlag(this IArg arg) =>
            arg.Info is FlagArgInfo info && info.IsNegatable;

        public static bool IsIntegerOption(this IArg arg) =>
            arg.Info is IntegerOptionArgInfo;

        public static bool IsMacro(this IArg arg) =>
            arg.Info is MacroArgInfo;

        public static bool IsOperand(this IArg arg) =>
            arg.Info is OperandArgInfo;

        public static string Name(this IArg arg) =>
            arg.Info switch
            {
                OptionArgInfo info => info.Name,
                FlagArgInfo info => info.Name,
                _ => null,
            };

        public static ShortOptionName ShortName(this IArg arg) =>
            arg.Info switch
            {
                OptionArgInfo info => info.ShortName,
                FlagArgInfo info => info.ShortName,
                _ => null,
            };

        public static string Description(this IArg arg) =>
            arg.Info.Description;

        public static T Description<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithInfo(arg.Info.WithDescription(value));

        public static IArg<T, OptionArgInfo> Name<T>(this IArg<T, OptionArgInfo> arg, string value) =>
            arg.WithInfo(arg.Info.WithName(value));

        public static IArg<T, OptionArgInfo> ShortName<T>(this IArg<T, OptionArgInfo> arg, char value) =>
            arg.ShortName(ShortOptionName.Parse(value));

        public static IArg<T, OptionArgInfo> ShortName<T>(this IArg<T, OptionArgInfo> arg, ShortOptionName value) =>
            arg.WithInfo(arg.Info.WithShortName(value));

        public static IArg<T, OptionArgInfo> DefaultValue<T>(this IArg<T, OptionArgInfo> arg) =>
            arg.WithInfo(arg.Info.WithIsValueOptional(true));

        public static IArg<T, OptionArgInfo> RequireValue<T>(this IArg<T, OptionArgInfo> arg) =>
            arg.WithInfo(arg.Info.WithIsValueOptional(false));

        public static IArg<T, FlagArgInfo> Name<T>(this IArg<T, FlagArgInfo> arg, string value) =>
            arg.WithInfo(arg.Info.WithName(value));

        public static IArg<T, FlagArgInfo> ShortName<T>(this IArg<T, FlagArgInfo> arg, char value) =>
            arg.ShortName(ShortOptionName.Parse(value));

        public static IArg<T, FlagArgInfo> ShortName<T>(this IArg<T, FlagArgInfo> arg, ShortOptionName value) =>
            arg.WithInfo(arg.Info.WithShortName(value));

        public static IArg<T, FlagArgInfo> Negatable<T>(this IArg<T, FlagArgInfo> arg, bool value) =>
            arg.WithInfo(arg.Info.WithIsNegatable(value));

        static IArg<T, TInfo>
            Create<T, TInfo>(TInfo info, Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) where TInfo : IArgInfo =>
            new Arg<T, TInfo>(info, accumulatorFactory, binder);

        public static IArg<bool, FlagArgInfo> Flag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Flag(ShortOptionName.Parse(s[0])),
                _ => Flag(name, null)
            };

        public static IArg<bool, FlagArgInfo> Flag(char shortName) =>
            Flag(ShortOptionName.Parse(shortName));

        public static IArg<bool, FlagArgInfo> Flag(ShortOptionName shortName) =>
            Flag(null, shortName);

        public static IArg<bool, FlagArgInfo> Flag(string name, ShortOptionName shortName) =>
            Create(new FlagArgInfo(name, shortName),
                   () => Accumulator.Value(Parser.Boolean("+", "-"), false, true, (_, v) => v),
                   r => r.Count > 0 && r.GetResult());

        public static IArg<int, FlagArgInfo> CountedFlag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => CountedFlag(ShortOptionName.Parse(s[0])),
                _ => CountedFlag(name, null)
            };

        public static IArg<int, FlagArgInfo> CountedFlag(char shortName) =>
            CountedFlag(ShortOptionName.Parse(shortName));

        public static IArg<int, FlagArgInfo> CountedFlag(ShortOptionName shortName) =>
            CountedFlag(null, shortName);

        public static IArg<int, FlagArgInfo> CountedFlag(string name, ShortOptionName shortName) =>
            Create(new FlagArgInfo(name, shortName),
                   Accumulator.Count, r => r.GetResult());

        public static IArg<(string Name, ImmutableArray<string> Args), MacroArgInfo> Macro(string name, Func<string, IEnumerable<string>> expander) =>
            Create(new MacroArgInfo(name, null),
                   () =>
                       Accumulator.Create(default((string, ImmutableArray<string>)),
                           (_, r) =>
                           {
                               if (!r.TryRead(out var s))
                                   return default;
                               var args = expander(s).ToImmutableArray();
                               foreach (var arg in args.Reverse())
                                   r.Unread(arg);
                               return ParseResult.Success((s, args));
                           },
                           delegate { throw new InvalidOperationException(); },
                           s => s),
                   r => r.Count > 0 ? r.GetResult() : default);

        public static IArg<T, OptionArgInfo> Option<T>(string name, T @default, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.Parse(s[0]), @default, parser),
                _ => Option(name, null, @default, parser)
            };

        public static IArg<T, OptionArgInfo> Option<T>(char shortName, T @default, IParser<T> parser) =>
            Option(ShortOptionName.Parse(shortName), @default, parser);

        public static IArg<T, OptionArgInfo> Option<T>(ShortOptionName shortName, T @default, IParser<T> parser) =>
            Option(null, shortName, @default, parser);

        public static IArg<T, OptionArgInfo> Option<T>(string name, ShortOptionName shortName, T @default, IParser<T> parser) =>
            Create(new OptionArgInfo(name, shortName), () => Accumulator.Value(parser, default(T), @default, (_, v) => v), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OptionArgInfo> Option<T>(string name, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.Parse(s[0]), parser),
                _ => Option(name, null, parser)
            };

        public static IArg<T, OptionArgInfo> Option<T>(char shortName, IParser<T> parser) =>
            Option(ShortOptionName.Parse(shortName), parser);

        public static IArg<T, OptionArgInfo> Option<T>(ShortOptionName shortName, IParser<T> parser) =>
            Option(null, shortName, parser);

        public static IArg<T, OptionArgInfo> Option<T>(string name, ShortOptionName shortName, IParser<T> parser) =>
            Option(name, shortName, default, parser);

        public static IArg<int, IntegerOptionArgInfo> IntOpt(string name) =>
            IntOpt(name, -1);

        public static IArg<int, IntegerOptionArgInfo> IntOpt(string name, int @default) =>
            IntOpt(name, @default, Parser.Int32());

        public static IArg<T, IntegerOptionArgInfo> IntOpt<T>(string name, IParser<T> parser) =>
            IntOpt(name, default, parser);

        public static IArg<T, IntegerOptionArgInfo> IntOpt<T>(string name, T @default, IParser<T> parser) =>
            Create(new IntegerOptionArgInfo(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OperandArgInfo> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<T, OperandArgInfo> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(new OperandArgInfo(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<ImmutableArray<T>, OperandArgInfo> List<T>(this IArg<T, OperandArgInfo> arg) =>
            List<T, OperandArgInfo>(arg);

        public static IArg<ImmutableArray<T>, OptionArgInfo> List<T>(this IArg<T, OptionArgInfo> arg) =>
            List<T, OptionArgInfo>(arg);

        public static IArg<ImmutableArray<T>, FlagArgInfo> List<T>(this IArg<T, FlagArgInfo> arg) =>
            List<T, FlagArgInfo>(arg);

        public static IArg<(bool Present, T Value), TInfo>
            FlagPresence<T, TInfo>(this IArg<T, TInfo> arg) where TInfo : IArgInfo =>
            arg.FlagPresence(false, true);

        public static IArg<(P Presence, T Value), TInfo>
            FlagPresence<T, P, TInfo>(this IArg<T, TInfo> arg, P absent, P present)
            where TInfo : IArgInfo =>
            Create(arg.Info,
                   () => from v in arg.CreateAccumulator()
                         select (Presence: present, Value: v),
                   r => r.Count > 0 ? (present, arg.Bind(() => Accumulator.Return(r.GetResult().Item2))) : (absent, default));

        static IArg<ImmutableArray<T>, TInfo> List<T, TInfo>(IArg<T, TInfo> arg)
            where TInfo : IArgInfo =>
            Create(arg.Info,
                   () =>
                       Accumulator.Create(ImmutableArray.CreateBuilder<T>(),
                           (array, args) =>
                           {
                               var accumulator = arg.CreateAccumulator();
                               if (!accumulator.Accumulate(args))
                                   return default;
                               array.Add(arg.Bind(() => accumulator));
                               return ParseResult.Success(array);
                           },
                           array =>
                           {
                               var accumulator = arg.CreateAccumulator();
                               accumulator.AccumulateDefault();
                               array.Add(arg.Bind(() => accumulator));
                               return array;
                           },
                           a => a.ToImmutable()),
                       r => r.Count > 0 ? r.GetResult() : ImmutableArray<T>.Empty);

        public static IArg<ImmutableArray<T>, TInfo> Tail<T, TInfo>(this IArg<T, TInfo> arg)
            where TInfo : IArgInfo =>
            Create(arg.Info,
                   () =>
                       Accumulator.Create(ImmutableArray<T>.Empty,
                           (seed, args) =>
                           {
                               var array = seed.ToBuilder();
                               while (args.HasMore())
                               {
                                   var accumulator = arg.CreateAccumulator();
                                   if (!accumulator.Accumulate(args))
                                       return default;
                                   array.Add(arg.Bind(() => accumulator));
                               }
                               return ParseResult.Success(array.ToImmutable());
                           },
                           r => r),
                   r => r.Count > 0 ? r.GetResult() : ImmutableArray<T>.Empty);
    }
}
