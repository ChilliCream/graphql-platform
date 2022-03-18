# How to contribute

One of the easiest ways to contribute is to participate in discussions on GitHub issues. You can also contribute by submitting pull requests with code changes.

## General feedback and discussions?

Start a discussion on the [repository issue tracker](https://github.com/ChilliCream/hotchocolate/issues) or [join us on slack](http://slack.chillicream.com/).

## Bugs and feature requests?

Before reporting a new issue, try to find an existing issue if one already exists. If it already exists, upvote (👍) it. Also consider adding a comment with your unique scenarios and requirements related to that issue.

If you can't find one, you can file a new issue by choosing the appropriate template [here](https://github.com/ChilliCream/hotchocolate/issues/new/choose).

## How to submit a pull request

We are always happy to see pull requests from community members both for bug fixes as well as new features.

### Finding an issue to work on

We have marked issues which are good candidates for first-time contributors, in case you are not already set on working on a specific issue.

- ["Good first issue" issues](https://github.com/ChilliCream/hotchocolate/labels/%F0%9F%99%8B%20good%20first%20issue) - we think these are a great for newcomers.
- ["Help wanted" issues](https://github.com/ChilliCream/hotchocolate/labels/%F0%9F%99%8B%20help%20wanted) - these issues are up for grabs.

### Before writing code

Before you spend time writing code, make sure of the following things:

- You have commented on the related issue to let others know you are working on it
- You have laid out your solution on a high level and received approval from the maintainers, if you are tackling a bigger change

After this you can fork our repository to implement your changes. If you are unfamiliar with forking, be sure to read [this guide](https://guides.github.com/activities/forking/) first.

To get started with the codebase, see [How to launch and build the solution](#how-to-launch-and-build-the-solution).

### Before submitting a pull request

Before submitting a pull request containing your changes, make sure that it checks the following requirements:

- You add test coverage following existing patterns within the codebase
- Your code matches the existing syntax conventions within the codebase
- You document any changes to the public API surface ([Learn more](./API-Baselines.md))
- Your pull request is small, focused, and avoids making unrelated changes

If your pull request contains any of the below, it's less likely to be merged.

- Changes that break backward compatibility
- Changes that are only wanted by one person/company
- Changes that add entirely new feature areas without prior agreement
- Changes that are mostly about refactoring existing code or code style

### Submitting a pull request

Follow [this guide](https://docs.github.com/en/github/collaborating-with-issues-and-pull-requests/creating-a-pull-request-from-a-fork) to submit your pull request. Be sure to mark it as draft if it is in an early stage.

### During pull request review

Core contributors will review your pull request and provide feedback.

## How to launch and build the solution

We use [Nuke](https://nuke.build/) for build automation. 

To work on Hot Chocolate, you will need .NET 6, Node 14, and Yarn 1.x.

After cloning the repository, run `init.sh` or `init.cmd`, which are located in the repository's root. The `build.ps1`, `build.cmd` or `build.sh` script files will create the `src/All.sln`, which can be used to develop in Visual Studio 2022 and higher or Rider 2021.3 EAP or higher.  It will also restore the packages for the documentation.

Other more focused solution files exist if you want to narrow in on a particular part of the platform.
The smaller solution files are great when working with VSCode.

The documentation is located in the `website` directory and can be started with `yarn start`.

There are other available commands too. As set up in the [.build](./.build/) directory.

## Code of conduct

See [CODE-OF-CONDUCT.md](./CODE-OF-CONDUCT.md)
