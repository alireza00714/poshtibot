using Namadno.AI.Support.Application.Abstractions;
using Namadno.AI.Support.Application.Abstractions.Analytics;
using Namadno.AI.Support.Application.Common;
using Namadno.AI.Support.Application.Copy;

namespace Namadno.AI.Support.Application.Analytics;

public sealed class SupportAnalyticsService(IUserContext user, ISupportAnalyticsQuery query, CopyTexts copy)
{
    public Task<SupportAnalyticsDto> GetAsync(CancellationToken cancellationToken)
    {
        if (!user.HasPermission(Permissions.AnalyticsRead))
        {
            throw new AppException(ErrorCodes.Forbidden, 403, copy.Errors.Forbidden);
        }

        return query.GetAsync(cancellationToken);
    }
}
