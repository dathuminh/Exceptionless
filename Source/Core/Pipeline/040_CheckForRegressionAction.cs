﻿#region Copyright 2014 Exceptionless

// This program is free software: you can redistribute it and/or modify it 
// under the terms of the GNU Affero General Public License as published 
// by the Free Software Foundation, either version 3 of the License, or 
// (at your option) any later version.
// 
//     http://www.gnu.org/licenses/agpl-3.0.html

#endregion

using System;
using CodeSmith.Core.Component;
using MongoDB.Bson;
using MongoDB.Driver.Builders;
using NLog.Fluent;

namespace Exceptionless.Core.Pipeline {
    [Priority(40)]
    public class CheckForRegressionAction : ErrorPipelineActionBase {
        private readonly ErrorStackRepository _errorStackRepository;

        public CheckForRegressionAction(ErrorStackRepository errorStackRepository) {
            _errorStackRepository = errorStackRepository;
        }

        protected override bool ContinueOnError { get { return true; } }

        public override void Process(ErrorPipelineContext ctx) {
            if (ctx.StackInfo == null || !ctx.StackInfo.DateFixed.HasValue || ctx.StackInfo.DateFixed.Value >= ctx.Error.OccurrenceDate.UtcDateTime)
                return;

            Log.Trace().Message("Marking error as an regression.").Write();
            _errorStackRepository.Collection.Update(
                Query.EQ(ErrorStackRepository.FieldNames.Id, new BsonObjectId(new ObjectId(ctx.StackInfo.Id))),
                Update
                    .Unset(ErrorStackRepository.FieldNames.DateFixed)
                    .Set(ErrorStackRepository.FieldNames.IsRegressed, true));

            _errorStackRepository.InvalidateCache(ctx.Error.ErrorStackId, ctx.StackInfo.SignatureHash, ctx.Error.ProjectId);
            ctx.IsRegression = true;
        }
    }
}