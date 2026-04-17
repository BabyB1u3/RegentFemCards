using System.Text.Json;
using PortraitModGenerator.Core.Abstractions;
using PortraitModGenerator.Core.Services;
using PortraitModGenerator.Gui.Resources;

namespace PortraitModGenerator.Gui;

internal sealed class BuildModForm : Form
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly Func<MergedReviewSession?> _sessionAccessor;
    private readonly Func<string?> _officialCardIndexPathAccessor;
    private readonly Func<string> _sessionRootAccessor;
    private readonly Action _synchronizeSession;
    private readonly BuildModDraft _draft;
    private readonly TextBox _modIdTextBox;
    private readonly TextBox _modNameTextBox;
    private readonly TextBox _authorTextBox;
    private readonly TextBox _descriptionTextBox;
    private readonly TextBox _outputDirectoryTextBox;
    private readonly Button _browseOutputButton;
    private readonly Button _buildButton;
    private readonly Label _statusLabel;
    private readonly ProgressBar _buildProgressBar;
    private readonly GroupBox _generationGroup;
    private readonly Label _modIdLabel;
    private readonly Label _modNameLabel;
    private readonly Label _authorLabel;
    private readonly Label _descriptionLabel;
    private readonly Label _artifactDirLabel;

    public BuildModForm(
        Func<MergedReviewSession?> sessionAccessor,
        Func<string?> officialCardIndexPathAccessor,
        Func<string> sessionRootAccessor,
        Action synchronizeSession,
        BuildModDraft draft)
    {
        _sessionAccessor = sessionAccessor;
        _officialCardIndexPathAccessor = officialCardIndexPathAccessor;
        _sessionRootAccessor = sessionRootAccessor;
        _synchronizeSession = synchronizeSession;
        _draft = draft;

        Text = Strings.BuildModForm_Title;
        Width = 840;
        Height = 420;
        MinimumSize = new Size(760, 380);
        StartPosition = FormStartPosition.CenterParent;

        TableLayoutPanel rootLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(12)
        };
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(rootLayout);

        _generationGroup = new GroupBox
        {
            Text = Strings.GroupBox_BuildSettings,
            Dock = DockStyle.Fill,
            Padding = new Padding(12)
        };
        rootLayout.Controls.Add(_generationGroup, 0, 0);

        TableLayoutPanel generationLayout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 7,
            AutoSize = true
        };
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        generationLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _generationGroup.Controls.Add(generationLayout);

        _modIdLabel = CreateFieldLabel(Strings.Label_ModId);
        generationLayout.Controls.Add(_modIdLabel, 0, 0);
        _modIdTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_modIdTextBox, 1, 0);

        _modNameLabel = CreateFieldLabel(Strings.Label_ModName);
        generationLayout.Controls.Add(_modNameLabel, 0, 1);
        _modNameTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_modNameTextBox, 1, 1);

        _authorLabel = CreateFieldLabel(Strings.Label_Author);
        generationLayout.Controls.Add(_authorLabel, 0, 2);
        _authorTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_authorTextBox, 1, 2);

        _descriptionLabel = CreateFieldLabel(Strings.Label_Description);
        generationLayout.Controls.Add(_descriptionLabel, 0, 3);
        _descriptionTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_descriptionTextBox, 1, 3);

        _artifactDirLabel = CreateFieldLabel(Strings.Label_ArtifactDir);
        generationLayout.Controls.Add(_artifactDirLabel, 0, 4);
        _outputDirectoryTextBox = CreateFieldTextBox();
        generationLayout.Controls.Add(_outputDirectoryTextBox, 1, 4);

        _browseOutputButton = new Button
        {
            Text = Strings.Button_Browse,
            AutoSize = true
        };
        _browseOutputButton.Click += (_, _) => BrowseOutputDirectory();
        generationLayout.Controls.Add(_browseOutputButton, 2, 4);

        _buildButton = new Button
        {
            Text = Strings.Button_BuildMod,
            AutoSize = true
        };
        _buildButton.Click += async (_, _) => await GenerateModProjectAsync();
        generationLayout.Controls.Add(_buildButton, 1, 5);

        _buildProgressBar = new ProgressBar
        {
            Dock = DockStyle.Top,
            Height = 18,
            Style = ProgressBarStyle.Marquee,
            MarqueeAnimationSpeed = 24,
            Visible = false,
            Margin = new Padding(0, 8, 0, 0)
        };
        generationLayout.Controls.Add(_buildProgressBar, 0, 6);
        generationLayout.SetColumnSpan(_buildProgressBar, 3);

        _statusLabel = new Label
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            MaximumSize = new Size(760, 0),
            Padding = new Padding(0, 4, 0, 0),
            Text = Strings.Help_BuildModPrompt
        };
        rootLayout.Controls.Add(_statusLabel, 0, 1);

        ApplyDraftToControls();

        LocalizationManager.LanguageChanged += HandleLanguageChanged;
        FormClosed += (_, _) => LocalizationManager.LanguageChanged -= HandleLanguageChanged;
    }

    private void HandleLanguageChanged()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(ApplyLocalization));
            return;
        }

        ApplyLocalization();
    }

    private void ApplyLocalization()
    {
        Text = Strings.BuildModForm_Title;
        _generationGroup.Text = Strings.GroupBox_BuildSettings;
        _modIdLabel.Text = Strings.Label_ModId;
        _modNameLabel.Text = Strings.Label_ModName;
        _authorLabel.Text = Strings.Label_Author;
        _descriptionLabel.Text = Strings.Label_Description;
        _artifactDirLabel.Text = Strings.Label_ArtifactDir;
        _browseOutputButton.Text = Strings.Button_Browse;
        _buildButton.Text = Strings.Button_BuildMod;
        _statusLabel.Text = Strings.Help_BuildModPrompt;
    }

    public void ApplySuggestedModId(string suggestedModId)
    {
        if (string.IsNullOrWhiteSpace(suggestedModId))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_draft.ModId) || string.Equals(_draft.ModId, "GeneratedPortraitMod", StringComparison.Ordinal))
        {
            _draft.ModId = suggestedModId;
        }

        if (string.IsNullOrWhiteSpace(_draft.ModName) || string.Equals(_draft.ModName, "GeneratedPortraitMod", StringComparison.Ordinal))
        {
            _draft.ModName = suggestedModId;
        }

        ApplyDraftToControls();
    }

    private void ApplyDraftToControls()
    {
        _modIdTextBox.Text = _draft.ModId;
        _modNameTextBox.Text = _draft.ModName;
        _authorTextBox.Text = _draft.Author;
        _descriptionTextBox.Text = _draft.Description;
        _outputDirectoryTextBox.Text = _draft.ArtifactOutputParent;
    }

    private void UpdateDraftFromControls()
    {
        _draft.ModId = _modIdTextBox.Text.Trim();
        _draft.ModName = _modNameTextBox.Text.Trim();
        _draft.Author = _authorTextBox.Text.Trim();
        _draft.Description = _descriptionTextBox.Text.Trim();
        _draft.ArtifactOutputParent = _outputDirectoryTextBox.Text.Trim();
    }

    private static Label CreateFieldLabel(string text)
    {
        return new Label
        {
            Text = text,
            AutoSize = true,
            Padding = new Padding(0, 8, 8, 0)
        };
    }

    private static TextBox CreateFieldTextBox()
    {
        return new TextBox
        {
            Dock = DockStyle.Fill,
            Width = 420
        };
    }

    private void SetBuildBusy(bool busy, string? statusText = null)
    {
        _buildProgressBar.Visible = busy;
        _buildButton.Enabled = !busy;
        _browseOutputButton.Enabled = !busy;
        if (statusText is not null)
        {
            _statusLabel.Text = statusText;
        }
    }

    private void BrowseOutputDirectory()
    {
        using FolderBrowserDialog dialog = new();
        if (!string.IsNullOrWhiteSpace(_outputDirectoryTextBox.Text) && Directory.Exists(_outputDirectoryTextBox.Text))
        {
            dialog.InitialDirectory = _outputDirectoryTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _outputDirectoryTextBox.Text = dialog.SelectedPath;
        }
    }

    private async Task GenerateModProjectAsync()
    {
        MergedReviewSession? session = _sessionAccessor();
        string? officialCardIndexPath = _officialCardIndexPathAccessor();
        if (session is null || string.IsNullOrWhiteSpace(officialCardIndexPath))
        {
            MessageBox.Show(this, Strings.Error_ImportAndReviewFirst, Strings.Dialog_BuildMod_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _synchronizeSession();
        session = _sessionAccessor();
        if (session is null)
        {
            return;
        }

        int pendingConflictCount = session.ConflictGroups.Count(group =>
            string.Equals(group.ResolutionState, "Pending", StringComparison.OrdinalIgnoreCase));
        if (pendingConflictCount > 0)
        {
            _statusLabel.Text = string.Format(Strings.Status_BuildBlocked, pendingConflictCount);
            MessageBox.Show(
                this,
                string.Format(Strings.Error_PendingConflicts, pendingConflictCount),
                Strings.Dialog_ResolveConflictsFirst_Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        int unmatchedCount = session.UnmatchedAssets;
        if (unmatchedCount > 0)
        {
            DialogResult pendingDecision = MessageBox.Show(
                this,
                string.Format(Strings.Warn_UnmatchedAssets, unmatchedCount),
                Strings.Dialog_UnmatchedAssets_Title,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (pendingDecision != DialogResult.Yes)
            {
                _statusLabel.Text = string.Format(Strings.Status_GenerationCancelled, unmatchedCount);
                return;
            }
        }

        UpdateDraftFromControls();

        if (string.IsNullOrWhiteSpace(_draft.ModId))
        {
            MessageBox.Show(this, Strings.Error_ModIdRequired, Strings.Dialog_BuildMod_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_draft.ArtifactOutputParent))
        {
            MessageBox.Show(this, Strings.Error_ArtifactDirRequired, Strings.Dialog_BuildMod_Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        string templateDirectory = AppPaths.PortraitTemplateDirectory;
        string sessionRoot = _sessionRootAccessor();
        string sourceGenerationRoot = Path.Combine(sessionRoot, "generated_src", _draft.ModId);
        string artifactOutputDirectory = Path.Combine(Path.GetFullPath(_draft.ArtifactOutputParent), _draft.ModId);
        string reviewPath = Path.Combine(sessionRoot, "merged", $"{_draft.ModId}.merged_review_session.json");
        string buildLogPath = Path.Combine(sessionRoot, "build", _draft.ModId, "publish.log");

        try
        {
            UseWaitCursor = true;
            SetBuildBusy(true, Strings.Status_PreparingBuild);

            Directory.CreateDirectory(Path.GetFullPath(_draft.ArtifactOutputParent));

            IProgress<string> progress = new Progress<string>(status => _statusLabel.Text = status);
            ModBuildResult buildResult = await Task.Run(() =>
            {
                progress.Report(Strings.Status_GeneratingSource);
                TemplateProjectGenerator templateGenerator = new();
                TemplateGenerationResult generationResult = templateGenerator.Generate(new TemplateGenerationRequest
                {
                    TemplateDirectory = templateDirectory,
                    OutputDirectory = sourceGenerationRoot,
                    OverwriteExistingOutput = true,
                    TokenValues = new Dictionary<string, string>(StringComparer.Ordinal)
                    {
                        ["__MOD_ID__"] = _draft.ModId,
                        ["__MOD_NAME__"] = string.IsNullOrWhiteSpace(_draft.ModName) ? _draft.ModId : _draft.ModName,
                        ["__AUTHOR__"] = string.IsNullOrWhiteSpace(_draft.Author) ? Strings.Default_UnknownAuthor : _draft.Author,
                        ["__DESCRIPTION__"] = string.IsNullOrWhiteSpace(_draft.Description) ? Strings.Default_ModDescription : _draft.Description,
                        ["__VERSION__"] = "v0.1.0"
                    }
                });

                progress.Report(Strings.Status_WritingReview);
                session.OfficialCardIndexPath = officialCardIndexPath;
                session.OutputJsonPath = reviewPath;
                File.WriteAllText(reviewPath, JsonSerializer.Serialize(session, JsonOptions));

                progress.Report(Strings.Status_Materializing);
                MappingMaterializer materializer = new();
                materializer.Materialize(new MaterializeMappingsRequest
                {
                    MappingAnalysisPath = reviewPath,
                    ModProjectRoot = sourceGenerationRoot,
                    ModId = _draft.ModId
                });

                progress.Report(Strings.Status_BuildingFinal);
                ModBuildService buildService = new();
                return buildService.Build(new ModBuildRequest
                {
                    ProjectFilePath = generationResult.EntryProjectPath,
                    ArtifactOutputDirectory = artifactOutputDirectory,
                    LogFilePath = buildLogPath,
                    DotnetCliHome = AppPaths.DotnetCliHome,
                    DotnetExecutablePath = AppPaths.DotnetExecutablePath,
                    RestoreConfigFilePath = AppPaths.NuGetConfigPath,
                    GodotExecutablePath = AppPaths.GodotExecutablePath
                });
            });

            _statusLabel.Text = string.Format(Strings.Status_BuiltTo, artifactOutputDirectory);
            MessageBox.Show(
                this,
                string.Format(Strings.Info_BuildSuccess, artifactOutputDirectory, buildResult.LogFilePath),
                Strings.Dialog_BuildMod_Title,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            string body = File.Exists(buildLogPath)
                ? string.Format(Strings.Info_BuildFailedWithLog, ex.Message, buildLogPath)
                : ex.Message;
            _statusLabel.Text = string.Format(Strings.Status_BuildFailed, buildLogPath);
            MessageBox.Show(this, body, Strings.Dialog_BuildModFailed_Title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBuildBusy(false);
            UseWaitCursor = false;
        }
    }
}
