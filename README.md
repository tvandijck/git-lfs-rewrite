git-lfs-rewrite
===============

Tool to rewrite history of an existing repository to use Git-LFS

Currently only supports a repository that exists of purely loose files, since the pack file parser isn't finished yet. 
But basically it goes through the entire history and finds files of a particular extension, and moves those files into 
the Git-LFS format. After this you should be able to push the repository to GitHub..

Unfortunately right now Git-LFS is so unstable and broken that the "git lfs push" command doesn't work, 
so actually pushing something just borks.

Oh well... we'll keep investigating as it updates I guess...


to unpack a repository into loose files:
in a msysgit bash shell:<br>
$  mkdir looseRepo<br>
$  cd looseRepo<br>
$  git init --bare<br>
$  for P in $(find <path_to_packed_repo> -name '*.pack'); do git unpack-objects < "$P"; done<br>


