﻿using System;
using System.Collections.Generic;
using System.Linq;
using BCLExtensions;
using Beefeater;
using csmacnz.Coveralls.Adapters;
using csmacnz.Coveralls.MetaDataResolvers;
using csmacnz.Coveralls.Ports;

namespace csmacnz.Coveralls
{
    public static class CoverageMetadataResolver
    {
        public static CoverageMetadata Resolve(MainArgs args, IEnvironmentVariables variables)
        {
            var resolvers = CreateResolvers(args, variables);
            var serviceName = Resolve(resolvers, r => r.ResolveServiceName());
            var serviceJobId = Resolve(resolvers, r => r.ResolveServiceJobId());
            var serviceNumber = Resolve(resolvers, r => r.ResolveServiceNumber());
            var pullRequestId = Resolve(resolvers, r => r.ResolvePullRequestId());
            var parallel = args.OptParallel;

            return new CoverageMetadata
            {
                ServiceJobId = serviceJobId.ValueOr("0"),
                ServiceName = serviceName.ValueOr("coveralls.net"),
                ServiceNumber = serviceNumber.ValueOr(null),
                PullRequestId = pullRequestId.ValueOr(null),
                Parallel = parallel
            };
        }

        private static List<IMetaDataResolver> CreateResolvers(MainArgs args, IEnvironmentVariables variables)
        {
            return new List<IMetaDataResolver>
            {
                new CommandLineMetaDataResolver(args),
                new AppVeyorMetaDataResolver(variables),
                new TravisMetaDataResolver(variables)
            };
        }

        private static Option<string> Resolve(List<IMetaDataResolver> resolvers, Func<IMetaDataResolver, Option<string>> resolve)
        {
            return resolvers
            .Where(r => r.IsActive())
            .Select(r => resolve?.Invoke(r) ?? Option<string>.None)
            .FirstOrDefault(v => v.HasValue);
        }
    }
}
