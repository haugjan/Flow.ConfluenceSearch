# Flow.ConfluenceSearch

Search for and open Confluence pages directly from Flow Launcher.

## Features

- 🔍 **Fast Content Search**: Search Confluence pages by title, excerpt, or content
- 🎯 **Space Filtering**: Configure default spaces for focused searches
- 🚀 **Direct Navigation**: Open pages directly in your browser
- ⚡ **Real-time Results**: Get instant search results as you type
- 🔧 **Configurable**: Customize timeout, result limits, and default spaces
- 👤 **User-based Search**: Find pages updated by specific users
- 🏷️ **Space Key Extraction**: Extract and display space keys from results
- 📊 **Browse URL Generation**: Generate URLs for direct access to pages

## Requirements

- Flow Launcher 2.0.0 or higher
- .NET 9.0 Runtime
- Valid Confluence account with API access

## Installation

### From Release
1. Download the latest release from the [Releases](../../releases) page.
2. Extract the zip file to your Flow Launcher plugins directory.
3. Restart Flow Launcher.

### From Source
1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/Flow.ConfluenceSearch.git
   ```
2. Build the project:
   ```bash
   cd Flow.ConfluenceSearch
   dotnet build --configuration Release
   ```
3. Copy the built files to Flow Launcher's plugin directory.

## Configuration

The plugin is configured through Flow Launcher's settings interface:

1. **Open Flow Launcher Settings**:
   - Press `Alt + Space` to open Flow Launcher.
   - Type `settings` and select "Flow Launcher Settings".

2. **Navigate to Plugin Settings**:
   - Go to the "Plugins" tab.
   - Find "Confluence Search" in the plugin list.
   - Click the settings icon next to it.

3. **Configure the following settings**:

   | Setting | Description | Example |
   |---------|-------------|---------|
   | **Base URL** | Your Confluence instance URL | `https://yourcompany.atlassian.net` |
   | **API Token** | Personal API token from Confluence | `ATATT3xFfGF0...` |
   | **Timeout** | Request timeout in seconds | `10` |
   | **Max Results** | Maximum number of results to display | `10` |
   | **Default Spaces** | Space keys to search by default | `["SPACE1", "SPACE2"]` |

### Creating a Confluence API Token

1. Go to [Atlassian Account Settings](https://id.atlassian.com/manage-profile/security/api-tokens).
2. Click "Create API token".
3. Give it a descriptive name (e.g., "Flow Launcher Plugin").
4. Copy the generated token and paste it in the plugin settings.

## Usage

### User-based Search
Open Flow Launcher (`Alt + Space`) and use the `confluence` keyword with these patterns:

**Updated by User:**
```
confluence @me
```
Find pages updated by me.

```
confluence @john
```
Find pages updated by John.

### Space-based Search

```
confluence #docs
```
Find pages from the "docs" space.

```
confluence #all
```
Search all spaces (ignores default space filter from settings).

### Text Search

```
confluence project plan
```
Search for pages containing "project" and "plan" in the title or content.

### Combined Search Examples

```
confluence @me #marketing
```
Find pages updated by me in the "marketing" space.

```
confluence @john #docs
```
Find pages updated by John in the "docs" space.

```
confluence #all meeting notes
```
Search all spaces for pages containing "meeting notes".

## Default Spaces

If you configure default spaces in the settings, searches will automatically be limited to those spaces unless you use `#all` or specify a different space with `#spacekey`.

## Search Operators Reference

| Operator | Description | Example |
|----------|-------------|---------|
| `@me` | Pages updated by current user | `confluence @me` |
| `@username` | Pages updated by specific user | `confluence @john` |
| `#spacekey` | Pages from specific space | `confluence #docs` |
| `#all` | Search all spaces | `confluence #all` |

## Troubleshooting

### No Results Appearing
- Verify your Confluence URL is correct and accessible.
- Check that your API token is valid and hasn't expired.
- Ensure you have permission to view the spaces you're searching.

### Timeout Issues
- Increase the timeout setting in plugin configuration.
- Check your internet connection to the Confluence instance.
- Verify the Confluence instance is responsive.

### Authentication Errors
- Regenerate your API token in Atlassian Account Settings.
- Ensure you're using your email address as the username (for Atlassian Cloud).
- Check that your account has appropriate permissions.

## Development

### Building from Source
```bash
git clone https://github.com/yourusername/Flow.ConfluenceSearch.git
cd Flow.ConfluenceSearch
dotnet restore
dotnet build --configuration Release
```

### Running Tests
```bash
dotnet test
```

## Contributing

1. Fork the repository.
2. Create a feature branch.
3. Make your changes.
4. Add tests if applicable.
5. Submit a pull request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

- **Issues**: Report bugs or request features on [GitHub Issues](../../issues).
- **Discussions**: Ask questions in [GitHub Discussions](../../discussions).
- **Documentation**: Check the [Wiki](../../wiki) for additional help.

## Acknowledgments

- Built for [Flow Launcher](https://flowlauncher.com/).
- Uses the Atlassian Confluence REST API.
- Icons from the Confluence design system.
