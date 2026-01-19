

error=0

for f in *.vert *.frag; do
    [ -e "$f" ] || continue
    if ! glslangValidator -V "$f" -o "$f.spv"; then error=1; fi
done

if [ $error -ne 0 ]; then exit 1; fi
