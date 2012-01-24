#!/bin/bash
#source /opt/mono-2.10.1/srcmono-2.10.1

set -o errexit -o nounset
set -o xtrace

# source variables.
. $1

# Check to see if already running, if so exit, if not lock and load
if [ -f "$MERGE_ERROR_FLAG" ]; then
	echo "Lock file found, exiting - please clean up merge and delete lock file $MERGE_ERROR_FLAG"
	exit 0
fi


cd $BASEDIR
MILESTONE=`$QUANTAGRATE -u=$USERNAME -p=$PASSWORD -s=$SERVER --project=$PROJECT milestone`
SAFE_MILESTONE=${MILESTONE// /_}
BRANCH="$SAFE_MILESTONE-ta"

annotate-output git fetch $REMOTE >> quantagit.log 2>&1

#always merge origin source branch in.
annotate-output git merge $REMOTE/$SOURCE -m "Merging $REMOTE/$SOURCE into $BRANCH" >> quantagit.log 2>&1

if ! git branch | grep "$BRANCH"
then
	annotate-output git checkout -B "$BRANCH" $SOURCE >> quantagit.log 2>&1
else
	annotate-output git checkout "$BRANCH" >> quantagit.log 2>&1
	annotate-output git merge $REMOTE/$BRANCH -m "Merging $REMOTE/$BRANCH into $BRANCH" >> quantagit.log 2>&1
fi

echo "$MILESTONE tickets resolved" > $CHANGES
echo "==========================" >> $CHANGES

$QUANTAGRATE -u=$USERNAME -p=$PASSWORD -s=$SERVER --project=$PROJECT -m="$MILESTONE" tickets |
while read ticket
do
	echo $ticket >> $CHANGES

	if git branch -a | grep $ticket >> quantagit.log 2>&1;
	then
		if ! annotate-output git merge $REMOTE/$ticket -m "Merging $REMOTE/$ticket into \"$BRANCH\"" >> quantagit.log 2>&1
		then
			echo "ERROR MERGING $REMOTE/$ticket"
			touch merge_error_flag
			exit 1
		fi
	elif git branch -a | grep "${ticket,,}";
	then
		if ! annotate-output git merge "$REMOTE/${ticket,,}" -m "Merging $REMOTE/$ticket into \"$BRANCH\"" >> quantagit.log 2>&1
		then
			echo "ERROR MERGING $REMOTE/${ticket,,}"
			touch merge_error_flag
			exit 1
		fi
	fi
done

if ! git diff --exit-code
then
	git add $CHANGES
	git commit -m "quantagit merged branches"
fi

annotate-output git push $REMOTE "$BRANCH" >> quantagit.log 2>&1

exit 0
