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

namespace Largs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;

    public interface IArg : IArgBinder
    {
        PropertySet Properties { get; }
        IArg WithProperties(PropertySet value);
        IAccumulator CreateAccumulator();
    }

    public interface IFlagArg    {}
    public interface IOptionArg  {}
    public interface IOperandArg {}
    public interface ITailArg    {}
    public interface IListArg    {}

    public interface IArg<out T, A> : IArg, IArgBinder<T>
    {
        new IArg<T, A> WithProperties(PropertySet value);
        new IAccumulator<T> CreateAccumulator();
    }

    public class Arg<T, A> : IArg<T, A>
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(IParser<T> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder) :
            this(PropertySet.Empty, parser, accumulatorFactory, binder) {}

        public Arg(PropertySet properties, IParser<T> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);
        IArg<T, A> IArg<T, A>.WithProperties(PropertySet value) => WithProperties(value);

        public Arg<T, A> WithProperties(PropertySet value) =>
            Properties == value ? this : new Arg<T, A>(value, Parser, _accumulatorFactory, _binder);

        public IParser<T> Parser { get; }

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();
        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        public Arg<(bool Present, T Value), A> FlagPresence() =>
            FlagPresence(false, true);

        public Arg<(TPresence Presence, T Value), A> FlagPresence<TPresence>(TPresence absent, TPresence present) =>
            new Arg<(TPresence, T), A>(
                Properties,
                from v in Parser select (present, v),
                () => from v in _accumulatorFactory()
                      select (Presence: present, Value: v),
                r => r.HasValue ? (present, _binder(Accumulator.Return(r.Value.Item2))) : (absent, default));

        object IArgBinder.Bind(Func<IArg, IAccumulator> source) => Bind(source);

        public T Bind(Func<IArg, IAccumulator> source) =>
            _binder((IAccumulator<T>)source(this));

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public static partial class Arg
    {
        public static class Symbols
        {
            public static readonly Symbol Name        = Symbol.New(nameof(Name));
            public static readonly Symbol Description = Symbol.New(nameof(Description));
        }

        public static string Name(this IArg arg) =>
            arg.Properties.Name();

        public static string Name(this PropertySet properties) =>
            (string)properties[Symbols.Name];

        public static T WithName<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithName(value));

        public static PropertySet WithName(this PropertySet properties, string value) =>
            properties.With(Symbols.Name, value);

        public static string Description(this IArg arg) =>
            (string)arg.Properties[Symbols.Description];

        public static string Description(this PropertySet properties) =>
            (string)properties[Symbols.Description];

        public static IArg WithDescription<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithDescription(value));

        public static PropertySet WithDescription(this PropertySet properties, string value) =>
            properties.With(Symbols.Description, value);
    }

    public partial class Arg
    {
        static readonly IOptionArg OptionArg   = null;
        static readonly IFlagArg FlagArg       = null;
        static readonly IOperandArg OperandArg = null;
        static readonly IListArg ListArg       = null;
        static readonly ITailArg TailArg       = null;

        static Arg<T, A>
            Create<T, A>(A _, IParser<T> parser,
                         Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) =>
            new Arg<T, A>(parser, accumulatorFactory, binder);

        public static Arg<bool, IFlagArg> Flag(string name) =>
            Create(FlagArg,
                   Parser.Create<bool>(_ => throw new NotSupportedException()),
                   () => from x in Accumulator.Flag() select x > 0,
                   r => r.HasValue)
                .WithName(name);

        public static Arg<T, IOptionArg> Option<T>(string name, T @default, IParser<T> parser) =>
            Create(OptionArg, parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default)
                .WithName(name);

        public static Arg<T, IOptionArg> Option<T>(string name, IParser<T> parser) =>
            Option(name, default, parser);

        public static Arg<T, IOperandArg> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(OperandArg, parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default);

        public static Arg<T, IOperandArg> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<ImmutableArray<T>, IListArg> List<T>(this IArg<T, IOperandArg> arg) =>
            List<T, IOperandArg>(arg);

        public static IArg<ImmutableArray<T>, IListArg> List<T>(this IArg<T, IOptionArg> arg) =>
            List<T, IOptionArg>(arg);

        public static IArg<ImmutableArray<T>, IListArg> List<T>(this IArg<T, IFlagArg> arg) =>
            List<T, IFlagArg>(arg);

        static IArg<ImmutableArray<T>, IListArg> List<T, A>(IArg<T, A> arg) =>
            Create(ListArg,
                   Parser.Create<ImmutableArray<T>>(_ => throw new NotImplementedException()),
                   () => Accumulator.Create(ImmutableArray<T>.Empty, (seed, args) =>
                   {
                       var array = seed.ToBuilder();
                       var accumulator = arg.CreateAccumulator();
                       if (!accumulator.Read(args))
                           return default;
                       array.Add(arg.Bind(_ => accumulator));
                       return ParseResult.Success(array.ToImmutable());
                   }),
                   r => r.Value)
                .WithProperties(arg.Properties);

        public static IArg<ImmutableArray<T>, ITailArg> Tail<T, A>(this IArg<T, A> arg)
            where A : IOperandArg =>
            Create(TailArg,
                   Parser.Create<ImmutableArray<T>>(_ => throw new NotImplementedException()),
                   () => Accumulator.Create(ImmutableArray<T>.Empty, (seed, args) =>
                   {
                       var array = seed.ToBuilder();
                       while (args.HasMore())
                       {
                           var reader = arg.CreateAccumulator();
                           if (!reader.Read(args))
                               return default;
                           array.Add(arg.Bind(_ => reader));
                       }
                       return ParseResult.Success(array.ToImmutable());
                   }),
                   r => r.Value)
                .WithProperties(arg.Properties);
    }
}
