import { LitElement, css, customElement, html } from "@umbraco-cms/backoffice/external/lit";
import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import "../shared/godmode-page.element";

const packages = [
    {
        name: "Diplo Translator Omni",
        url: "https://translator.diplo.co.uk/",
        description: "Translate pages, blocks and dictionary items directly inside the Umbraco backoffice using AI, with tools to keep translations up to date."
    },
    {
        name: "Diplo Audit Log Viewer",
        url: "https://www.diplo.co.uk/packages/audit-log-viewer/",
        description: "Explore your Umbraco audit logs through a dedicated backoffice viewer."
    },
    {
        name: "Diplo Dictionary Translator",
        url: "https://www.diplo.co.uk/articles/diplo-dictionary-translator/",
        description: "A free tool for translating Umbraco dictionary items using Umbraco AI."
    }
];

@customElement("godmode-about")
export class GodModeAboutElement extends UmbElementMixin(LitElement) {
    override render() {
        return html`
            <godmode-page heading="About God Mode" icon="icon-user" description="Deus Ex Machina: The developer, contributions and a few more useful Umbraco packages.">
                <div class="about">
                    <uui-box>
                        <h2>Hi, I'm Dan “Diplo” Booth</h2>
                        <p>God Mode is created and maintained by me, an Umbraco developer and package author.</p>
                        <p>I build tools that help developers and editors get more from Umbraco. Away from the keyboard, I also make music and take photographs.</p>
                        <nav aria-label="Find Dan online">
                            <a href="https://www.diplo.co.uk/" target="_blank" rel="noopener noreferrer">Visit my website <span aria-hidden="true">↗</span></a>
                            <a href="https://github.com/DanDiplo" target="_blank" rel="noopener noreferrer">Find me on GitHub <span aria-hidden="true">↗</span></a>
                        </nav>
                    </uui-box>

                    <uui-box>
                        <h2>Help improve God Mode</h2>
                        <p>Found a bug or have an idea for a useful feature? Issues, suggestions and contributions are welcome.</p>
                        <nav aria-label="Contribute to God Mode">
                            <a href="https://github.com/DanDiplo/Umbraco.GodMode/issues" target="_blank" rel="noopener noreferrer">Report an issue or suggest a feature <span aria-hidden="true">↗</span></a>
                            <a href="https://github.com/DanDiplo/Umbraco.GodMode/" target="_blank" rel="noopener noreferrer">Explore the source and contribute <span aria-hidden="true">↗</span></a>
                        </nav>
                    </uui-box>

                    <section aria-labelledby="more-packages">
                        <h2 id="more-packages">More Umbraco packages by me</h2>
                        <div class="packages">
                            ${packages.map((pkg) => html`
                                <uui-box>
                                    <h3><a href=${pkg.url} target="_blank" rel="noopener noreferrer">${pkg.name} <span aria-hidden="true">↗</span></a></h3>
                                    <p>${pkg.description}</p>
                                </uui-box>
                            `)}
                        </div>
                    </section>
                </div>
            </godmode-page>
        `;
    }

    static override styles = css`
        .about {
            display: grid;
            gap: var(--uui-size-space-5);
            max-width: 1120px;
            margin: 0 auto;
        }
        h2 {
            font-size: var(--uui-type-h4-size, 20px);
            line-height: 1.4;
            margin: 0 0 var(--uui-size-space-4);
        }
        h3 {
            font-size: 1rem;
            line-height: 1.5;
            margin: 0 0 var(--uui-size-space-3);
        }
        p {
            color: var(--uui-color-text-alt);
            line-height: 1.6;
            margin: 0 0 var(--uui-size-space-4);
            max-width: 80ch;
        }
        nav {
            display: flex;
            flex-wrap: wrap;
            gap: var(--uui-size-space-3) var(--uui-size-space-6);
        }
        a {
            color: var(--uui-color-interactive);
            text-decoration: underline;
            text-underline-offset: 3px;
        }
        a:hover {
            color: var(--uui-color-interactive-emphasis);
        }
        a:focus-visible {
            outline: 2px solid var(--uui-color-interactive);
            outline-offset: 4px;
        }
        .packages {
            display: grid;
            grid-template-columns: repeat(3, minmax(0, 1fr));
            gap: var(--uui-size-space-4);
        }
        .packages uui-box {
            min-width: 0;
        }
        .packages p {
            margin-bottom: 0;
        }
        @media (max-width: 1000px) {
            .packages {
                grid-template-columns: 1fr;
            }
        }
    `;
}

export default GodModeAboutElement;

declare global {
    interface HTMLElementTagNameMap {
        "godmode-about": GodModeAboutElement;
    }
}
