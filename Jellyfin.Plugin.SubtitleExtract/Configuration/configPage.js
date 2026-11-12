const pluginId = "CD893C24-B59E-4060-87B2-184070E1BF68";

function generateCheckboxList(page, items, selectedItems, containerId) {
    const container = page.querySelector('#' + containerId);
    const selected = selectedItems || [];
    const fragment = document.createDocumentFragment();
    for (const item of items || []) {
        const label = document.createElement("label");
        label.className = "emby-checkbox-label";

        // Built with properties rather than markup so names containing ", < or &
        // render and round-trip unchanged.
        // emby-checkbox is upgraded from the is attribute once it is in the document.
        const input = document.createElement("input");
        input.setAttribute("is", "emby-checkbox");
        input.type = "checkbox";
        input.value = item.Value;
        input.checked = selected.includes(item.Value);

        const span = document.createElement("span");
        span.className = "checkboxLabel";
        span.textContent = item.Text;

        label.appendChild(input);
        label.appendChild(span);
        fragment.appendChild(label);
    }
    container.innerHTML = "";
    container.appendChild(fragment);
}

async function populateLibraries(page, config) {
    const response = await window.ApiClient.getVirtualFolders();
    const tvLibraries = response.filter((item) => item.CollectionType === undefined || item.CollectionType === "tvshows" || item.CollectionType === "movies");
    const libraryNames = tvLibraries.map(lib => ({ Value: lib.Name, Text: lib.Name }));
    generateCheckboxList(page, libraryNames, config.SelectedSubtitlesLibraries, "librarySubtitlesCheckboxes");
    generateCheckboxList(page, libraryNames, config.SelectedAttachmentsLibraries, "libraryAttachmentsCheckboxes");
}

function toggleMode(page, isAdvanced) {
    const easyModeSection = page.querySelector('#easyModeSection');
    const codecsDetails = page.querySelector('#codecsDetails');

    if (isAdvanced) {
        easyModeSection.style.display = 'none';
        codecsDetails.style.display = 'block';
    } else {
        easyModeSection.style.display = 'block';
        codecsDetails.style.display = 'none';
    }
}

function getChecked(root, wrapperId)
{
    const nodes = root.querySelector('#' + wrapperId).querySelectorAll('input:checked');
    let values = [];

    for (const node of nodes)
    {
        values.push(node.value);
    }

    return values;
}

function onRequestError(err) {
    console.error('[SubtitleExtract] configuration request failed', err);
    // processErrorResponse hides the loading spinner and expects a fetch Response.
    Dashboard.processErrorResponse(err instanceof Response ? err : { statusText: String(err) });
}

// The dashboard calls this once per page element it creates; a cached view that
// is shown again only receives another viewshow, so listeners are bound once.
export default function (view) {
    view.addEventListener('viewshow', function () {

        Dashboard.showLoadingMsg();

        ApiClient.getPluginConfiguration(pluginId).then(async function (config) {
            await populateLibraries(view, config);

            view.querySelector('#chkEnableDuringScan').checked = !!config.ExtractionDuringLibraryScan;
            view.querySelector('#chkTextSubtitles').checked = !!config.IncludeTextSubtitles;
            view.querySelector('#chkGraphicalSubtitles').checked = !!config.IncludeGraphicalSubtitles;
            view.querySelector('#chkAdvancedMode').checked = !!config.IsAdvancedMode;

            generateCheckboxList(view, config.AllSubtitleCodecs, config.SelectedCodecs, "includedCodecsCheckboxes");

            const isAdvanced = config.IsAdvancedMode === true;
            toggleMode(view, isAdvanced);

            Dashboard.hideLoadingMsg();
        }).catch(onRequestError);
    });

    view.querySelector('.extractorConfigurationForm').addEventListener('submit', function (e) {
        e.preventDefault();

        Dashboard.showLoadingMsg();

        const form = e.currentTarget;

        ApiClient.getPluginConfiguration(pluginId).then(function (config) {
            config.ExtractionDuringLibraryScan = form.querySelector('#chkEnableDuringScan').checked;
            config.IncludeTextSubtitles = form.querySelector('#chkTextSubtitles').checked;
            config.IncludeGraphicalSubtitles = form.querySelector('#chkGraphicalSubtitles').checked;
            config.IsAdvancedMode = form.querySelector('#chkAdvancedMode').checked;
            config.SelectedSubtitlesLibraries = getChecked(form, 'librarySubtitlesCheckboxes');
            config.SelectedAttachmentsLibraries = getChecked(form, 'libraryAttachmentsCheckboxes');
            config.SelectedCodecs = getChecked(form, 'includedCodecsCheckboxes');

            return ApiClient.updatePluginConfiguration(pluginId, config).then(Dashboard.processPluginConfigurationUpdateResult);
        }).catch(onRequestError);
    });

    view.querySelector('#chkAdvancedMode').addEventListener('change', function (e) {
        const isAdvanced = e.target.checked;
        toggleMode(view, isAdvanced);
    });
}
