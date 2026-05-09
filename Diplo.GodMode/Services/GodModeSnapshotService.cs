using Diplo.GodMode.Models;
using Diplo.GodMode.Services.Interfaces;

namespace Diplo.GodMode.Services
{
    public class GodModeSnapshotService : IGodModeSnapshotService
    {
        private readonly IUmbracoDataService dataService;
        private readonly IUmbracoDatabaseService dataBaseService;
        private readonly IGodModeHealthRiskService healthRiskService;

        public GodModeSnapshotService(
            IUmbracoDataService dataService,
            IUmbracoDatabaseService dataBaseService,
            IGodModeHealthRiskService healthRiskService)
        {
            this.dataService = dataService;
            this.dataBaseService = dataBaseService;
            this.healthRiskService = healthRiskService;
        }

        public async Task<GodModeSnapshot> CreateSchemaHealthSnapshotAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contentTypesTask = Task.FromResult(dataService.GetContentTypeMap().ToList());
            var dataTypesTask = dataService.GetDataTypesStatus();
            var referenceGraphTask = dataService.GetReferenceGraph();
            var templatesTask = dataService.GetTemplates();
            var driftFindingsTask = dataService.GetConfigurationDriftFindings();
            var healthFindingsTask = healthRiskService.BuildHealthRiskFindingsAsync(cancellationToken);
            var orphanedTagsTask = Task.FromResult(dataBaseService.GetOrphanedTags());
            var orphanedMediaTask = Task.FromResult(dataBaseService.GetOrphanedMedia());

            await Task.WhenAll(dataTypesTask, referenceGraphTask, templatesTask, driftFindingsTask, healthFindingsTask);
            cancellationToken.ThrowIfCancellationRequested();

            return new GodModeSnapshot
            {
                ContentTypes = contentTypesTask.Result,
                DataTypes = (await dataTypesTask).ToList(),
                ReferenceGraph = (await referenceGraphTask).ToList(),
                Templates = (await templatesTask).ToList(),
                ConfigurationDriftFindings = (await driftFindingsTask).ToList(),
                HealthRiskFindings = (await healthFindingsTask).ToList(),
                OrphanedTags = orphanedTagsTask.Result,
                OrphanedMedia = orphanedMediaTask.Result
            };
        }
    }
}
