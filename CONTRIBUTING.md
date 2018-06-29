Contributing to XMLUnit
=======================

Contributing to XMLUnit 2.x
---------------------------

If you think you've found a bug or are missing a feature, please
[open an issue](https://github.com/xmlunit/xmlunit.net/issues), we'll
also gladly accept
[pull requests](https://github.com/xmlunit/xmlunit.net/pulls).

Before you start working on a big feature, please tell us about it on
the mailing list, though.  This way you can make sure you're not
wasting your time on something that isn't considered to be in
XMLUnit's scope.

If you are looking for something to work on, we've compiled a
[list](https://github.com/xmlunit/xmlunit/blob/master/HELP_WANTED.md) of things
that should be done before XMLUnit 2.0 can be released.

XMLUnit's users guide is developed (or rather will be developed)
inside [a github Wiki](https://github.com/xmlunit/user-guide/wiki) -
a lot of help is needed here, before we can cut a new release.

### Preparing a Pull Request

+ Create a topic branch from where you want to base your work (this is
  usually the master branch).
+ Make commits of logical units.
+ Respect the original code style:
  + Only use spaces for indentation.
  + Create minimal diffs - disable on save actions like reformat
    source code or organize imports. If you feel the source code
    should be reformatted create a separate issue/PR for this change.
  + Check for unnecessary whitespace with `git diff --check` before committing.
+ Make sure your commit messages are in the proper format. Your commit
  message should contain the key of the issue if you created one.
+ Make sure you have added the necessary tests for your changes.
+ Run all the tests with `msbuild XMLUnit.NET.sln` or `xbuild
  XMLUnit.NET.sln` to assure nothing else was accidentally broken.

Contributing to XMLUnit for .NET 0.x
------------------------------------

We don't expect to work on the old XMLUnit for .NET anymore, please
help us getting version 2.x on the road as soon as possible.
