# Contributing to MsDbServer

Thank you for your interest in contributing to MsDbServer! We welcome contributions from the community.

## How to Contribute

### 1. Fork the Repository

- Fork the repository on GitHub
- Clone your fork locally: `git clone https://github.com/your-username/MsDbServer.git`

### 2. Set Up Development Environment

- Install .NET 8 SDK
- Install Visual Studio Code (recommended)
- Set up a MySQL database for testing

### 3. Make Changes

- Create a feature branch: `git checkout -b feature-name`
- Make your changes
- Test your changes thoroughly
- Ensure code follows existing patterns

### 4. Testing

- Test with a real MySQL database
- Verify all MCP tools work correctly
- Test GitHub Copilot integration

### 5. Submit Pull Request

- Push your changes to your fork
- Submit a pull request with a clear description

## Development Guidelines

### Code Style

- Follow C# coding conventions
- Use meaningful variable and method names
- Add XML documentation comments for public methods
- Keep methods focused and single-purpose

### Database Safety

- All database operations must be async
- Sanitize all inputs
- Use parameterized queries
- Implement proper error handling

### MCP Tools

- Use appropriate MCP attributes
- Provide clear descriptions
- Include parameter descriptions
- Return formatted, readable output

## Areas for Contribution

### 🚀 New Features

- Support for other database types (PostgreSQL, SQL Server)
- Additional database introspection tools
- Performance optimizations
- Enhanced error messages

### 🐛 Bug Fixes

- Database connection issues
- Query execution problems
- MCP integration improvements

### 📚 Documentation

- Setup guides for different operating systems
- Video tutorials
- Example use cases
- API documentation

### 🧪 Testing

- Unit tests
- Integration tests
- Performance benchmarks

## Questions?

Feel free to open an issue for:

- Bug reports
- Feature requests
- Questions about usage
- Discussion about improvements

Thank you for contributing! 🎉
