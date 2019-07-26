(function () {
    'use strict';
    angular.module("umbraco").controller("GodMode.TypesIntro.Controller",
        function ($routeParams, navigationService, godModeConfig) {

            navigationService.syncTree({ tree: $routeParams.tree, path: ["reflectionTree"], forceReload: false });

            var vm = this;
            vm.config = godModeConfig.config;

            vm.pages = [
                { name: "Surface Controllers", url: "reflectionBrowser/surface", desc: "Browse Umbraco Surface Controllers" },
                { name: "API Controllers", url: "reflectionBrowser/api", desc: "Browse Umbraco Web API Controllers" },
                { name: "Render Controllers", url: "reflectionBrowser/render", desc: "Browse Umbraco Render MVC Controllers" },
                { name: "Content Models", url: "reflectionBrowser/models", desc: "Browse Umbraco Content Models" },
                { name: "Composers", url: "reflectionBrowser/composers", desc: "Browse Umbraco Composers" },
                { name: "Value Converters", url: "reflectionBrowser/converters", desc: "Browse Property Value Controllers" },
                { name: "Interface Browser", url: "typeBrowser", desc: "Browse Intrfaces and derived types in your site" }
            ];

        });
})();