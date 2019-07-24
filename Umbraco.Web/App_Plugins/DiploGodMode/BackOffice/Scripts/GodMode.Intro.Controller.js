(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.Intro.Controller",
        function ($routeParams, navigationService, godModeConfig) {

            var vm = this;
            vm.config = godModeConfig.config;

            console.log($routeParams);

            navigationService.syncTree({ tree: $routeParams.tree, path: [], forceReload: false });

            vm.pages = [
                { name: "DocType Browser", url: "docTypeBrowser", desc: "Browse, filter and search document types and see where they are used" },
                { name: "Template Browser", url: "templateBrowser", desc: "Filter, browse and search the template hierarchy and see what partials they use" },
                { name: "Partial Browser", url: "partialBrowser", desc: "Browse partial views and see whether they are cached" },
                { name: "DataType Browser", url: "dataTypeBrowser", desc: "Browse data types, see whether they are used and by which editor" },
                { name: "Content Browser", url: "contentBrowser", desc: "Browse, search and filter all your content pages and see which languages are assigned" },
                { name: "Usage Browser", url: "usageBrowser", desc: "See how your content types are used and how many instances have been made" },
                { name: "Media Browser", url: "mediaBrowser", desc: "Search your media and filter by type" },
                { name: "Member Browser", url: "memberBrowser", desc: "Search members and see what groups they have been assigned to" },
                { name: "Type Browser", url: "typesIntro", desc: "See how controllers, composers and models are made up and browse interfaces" },
                { name: "Diagnostics", url: "diagnosticBrowser", desc: "View Umbraco settings and configuration, Server settings and much more..." },
                { name: "Utlities", url: "utilityBrowser", desc: "Clear caches, restart application pool and warm-up your little templates" }
            ];

        });
})();