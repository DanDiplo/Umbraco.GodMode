﻿(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.Intro.Controller",
        function ($routeParams, navigationService, godModeResources) {

            const vm = this;
            vm.pages = [];

            godModeResources.getConfig().then(function (config) {

                const pages = [
                    { name: "DocType Browser", url: "docTypeBrowser", desc: "Browse, filter and search document types and see where they are used" },
                    { name: "Template Browser", url: "templateBrowser", desc: "Filter, browse and search the template hierarchy and see what partials they use" },
                    { name: "Partial Browser", url: "partialBrowser", desc: "Browse partial views and see whether they are cached" },
                    { name: "DataType Browser", url: "dataTypeBrowser", desc: "Browse data types, see whether they are used and by which editor" },
                    { name: "Content Browser", url: "contentBrowser", desc: "Browse, search and filter all your content pages" },
                    { name: "Usage Browser", url: "usageBrowser", desc: "See how your content types are used and how many instances have been made" },
                    { name: "Media Browser", url: "mediaBrowser", desc: "Search your media and filter by type" },
                    { name: "Member Browser", url: "memberBrowser", desc: "Search members and see what groups they have been assigned to" },
                    { name: "Tag Browser", url: "tagBrowser", desc: "View all tags and see what content they are assigned to" },
                    { name: "Type Browser", url: "typesIntro", desc: "See how controllers, composers and models are made up and browse interfaces" },
                    { name: "Services", url: "serviceBrowser", desc: "Browse injected services registered with the IOC container." },
                    { name: "Diagnostics", url: "diagnosticBrowser", desc: "View Umbraco settings and configuration, Server settings and much more..." },
                    { name: "Utlities", url: "utilityBrowser", desc: "Clear caches, restart application pool and warm-up your little templates" }
                ];

                // prune features based on config

                for (let i = 0; i < pages.length; i++) {
                    var page = pages[i];
                    if (config.FeaturesToHide.includes(page.name) || config.FeaturesToHide.includes(page.url)) {
                        continue;
                    }
                    vm.pages.push(page);
                }

            });

            navigationService.syncTree({ tree: $routeParams.tree, path: [], forceReload: false });

        });
})();