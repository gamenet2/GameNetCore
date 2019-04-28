// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

namespace Contoso.GameNetCore.Server.Kestrel.Core.Internal.Proto2
{
    internal interface IProto2StreamLifetimeHandler
    {
        void OnStreamCompleted(Proto2Stream stream);
    }
}
