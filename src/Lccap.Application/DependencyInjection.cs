using Lccap.Application.Actions.Commands;
using Lccap.Application.Actions.Queries;
using Lccap.Application.Audit.Queries;
using Lccap.Application.Common;
using Lccap.Application.Common.Interfaces;
using Lccap.Application.Documents.Commands;
using Lccap.Application.Documents.Queries;
using Lccap.Application.Export.Commands;
using Lccap.Application.Export.Queries;
using Lccap.Application.Monitoring.Commands;
using Lccap.Application.Plans.Commands;
using Lccap.Application.Plans.Queries;
using Lccap.Application.Sections.Commands;
using Lccap.Application.Sections.Queries;
using Lccap.Application.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Lccap.Application;

public static class DependencyInjection
{
    public static void AddApplicationServices(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        _ = services.AddSingleton<IClock, SystemClock>();
        _ = services.AddScoped<ICurrentUserContext, CurrentUserContext>();
        _ = services.AddScoped<CreateIndicatorCommand>();
        _ = services.AddScoped<UpdateMonitoringIndicatorCommand>();
        _ = services.AddScoped<ArchiveMonitoringIndicatorCommand>();
        _ = services.AddScoped<CreatePlanCommand>();
        _ = services.AddScoped<UpdatePlanCommand>();
        _ = services.AddScoped<ArchivePlanCommand>();
        _ = services.AddScoped<GetPlanByIdQuery>();
        _ = services.AddScoped<GetPlansQuery>();
        _ = services.AddScoped<UploadDocumentCommand>();
        _ = services.AddScoped<UpdateDocumentMetadataCommand>();
        _ = services.AddScoped<ArchiveDocumentCommand>();
        _ = services.AddScoped<CreateExportJobCommand>();
        _ = services.AddScoped<DownloadExportQuery>();
        _ = services.AddScoped<GetDocumentsByPlanQuery>();
        _ = services.AddScoped<GetEvidenceIndexByPlanQuery>();
        _ = services.AddScoped<SavePlanSectionCommand>();
        _ = services.AddScoped<RestorePlanSectionCommand>();
        _ = services.AddScoped<GetPlanSectionsQuery>();
        _ = services.AddScoped<GetPlanSectionByKeyQuery>();
        _ = services.AddScoped<GetPlanSectionHistoryQuery>();
        _ = services.AddScoped<GetSectionCommentsQuery>();
        _ = services.AddScoped<CreateSectionCommentCommand>();
        _ = services.AddScoped<ResolveSectionCommentCommand>();
        _ = services.AddScoped<ReopenSectionCommentCommand>();
        _ = services.AddScoped<ArchiveSectionCommentCommand>();
        _ = services.AddScoped<CreateActionItemCommand>();
        _ = services.AddScoped<UpdateActionItemCommand>();
        _ = services.AddScoped<ArchiveActionItemCommand>();
        _ = services.AddScoped<GetActionItemsByPlanQuery>();
        _ = services.AddScoped<GetActionItemByIdQuery>();
        _ = services.AddScoped<GetAuditLogsQuery>();

        // Auth / session services (Slice 2)
        _ = services.AddScoped<RefreshTokenService>();
        _ = services.AddScoped<AuthSessionService>();
    }
}
