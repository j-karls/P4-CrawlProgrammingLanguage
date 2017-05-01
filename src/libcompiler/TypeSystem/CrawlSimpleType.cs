﻿using System;

namespace libcompiler.TypeSystem
{
    public class CrawlSimpleType : CrawlType
    {
        public static CrawlSimpleType Tal { get; } = new CrawlSimpleType(typeof(int));

        public static CrawlSimpleType Kommatal { get; } = new CrawlSimpleType(typeof(double));

        public static CrawlSimpleType Bool { get; } = new CrawlSimpleType(typeof(bool));

        public static CrawlSimpleType Tegn { get; } = new CrawlSimpleType(typeof(char));

        public static CrawlSimpleType Tekst { get; } = new CrawlSimpleType(typeof(string));

        public CrawlSimpleType(string identifier, string @namespace, string assembly = "CrawlCode") : base(identifier, @namespace, assembly)
        {
        }

        private CrawlSimpleType(Type type) : base(type.FullName , type.Namespace, type.Assembly.FullName)
        {
        }

        public override bool IsAssignableTo(CrawlType target)
        {
            return Equals(target);
        }

        public override bool ImplicitlyCastableTo(CrawlType target)
        {
            throw new NotImplementedException();
        }

        public override bool CastableTo(CrawlType target)
        {
            throw new NotImplementedException();
        }
    }
}