# Contributing

When contributing to this repository, please first discuss the change you wish to make via issue,
email, or any other method with the owners of this repository before making a change. 

We don't maintain a Contributor License Agreement (CLA) but we do require that anyone that wishes to contribute agrees to the following:

* You have the right to assign the copyright of your contribution.
* By making your contribution, you are giving up copyright of your contribution.

This application is released under the [AGPL 3.0](https://github.com/slskd/slskd/blob/master/LICENSE) license, and no single individual or entity owns
or will ever own the copyright.

## slskdn Fork Attribution Policy

This is a fork of [slskd/slskd](https://github.com/slskd/slskd). We maintain the following copyright attribution policy:

### Existing Files (from upstream slskd)
Files that exist in the upstream repository retain their original copyright:
```csharp
// <copyright file="Example.cs" company="slskd Team">
//     Copyright (c) slskd Team. All rights reserved.
```

### New Files (slskdn-specific)
New files created specifically for slskdn features use the fork's copyright:
```csharp
// <copyright file="Example.cs" company="slskdn Team">
//     Copyright (c) slskdn Team. All rights reserved.
```

### Fork-Specific Directories
The following directories contain slskdn-specific code:
- `src/slskd/Capabilities/` - Peer capability discovery
- `src/slskd/HashDb/` - Local hash database
- `src/slskd/Mesh/` - Epidemic mesh sync protocol
- `src/slskd/Backfill/` - Conservative header probing
- `src/slskd/Transfers/MultiSource/` - Multi-source swarm downloads
- `src/slskd/Transfers/Ranking/` - Source ranking
- `src/slskd/Users/Notes/` - User notes

When creating new files, use the slskdn Team copyright header. When modifying existing upstream files, preserve the original slskd Team copyright.

## Contribution Workflow

1. Assign yourself to the issue that you'll be working on.  If you'd like to contribute something for which there is no 
   existing issue, consider creating one before you start so we can discuss.
1. Clone the repository and `git checkout master` to ensure you are on the master branch.
1. Create a new branch for your change with `git checkout -b <your-branch-name>` be descriptive, but terse.
1. Make your changes.  When finished, push your branch with `git push origin --set-upstream <your-branch-name>`.
1. Create a pull request to merge `<your-branch-name>` into `master`.
1. A maintainer will review your pull request and may make comments, ask questions, or request changes.  When all
   feedback has been addressed the pull request will be approved, and after all checks have passed it will be merged by
   a maintainer, or you may merge it yourself if you have the necessary access.
1. Delete your branch, unless you plan to submit additional pull request from it.

Note that we require that all branches are up to date with target branch prior to merging.  If you see a message about this
on your pull request, use `git fetch` to retrieve the latest changes,  `git merge origin/master` to merge the changes from master
into your local branch, then `git push` to update your branch.

## Environment Setup

You'll need [.NET 8.0](https://dotnet.microsoft.com/en-us/download) to build and run the back end (slskd), and you'll 
need [Nodejs](https://nodejs.org/en/) to build and debug the front end (web).

You're free to use whichever development tools you prefer.  If you don't yet have a preference, we recommend the following:

[Visual Studio Code](https://code.visualstudio.com/) for front or back end development.

[Visual Studio](https://visualstudio.microsoft.com/downloads/) for back end development.

## Debugging

### Back End

Run `./bin/watch` from the root of the repository.

### Front End

Run `./bin/watch --web` from the root of the directory.  Make sure the back end is running first.
