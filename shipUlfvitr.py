import shutil
import os
import json


# Parameters
target_name = "Ulfvitr, The Wise Wolf"
csproj_name = "TheWiseWolf"

enabled = False
load_to_local_ato = True


# path to source directory
script_dir = os.path.dirname(__file__)
char_name = script_dir.split("/")[-1]


config_dir = script_dir + f"/{char_name}Configs"

# path to destination directory
config_dest_dir = os.path.abspath(os.path.join(script_dir, '..', f'Ready to Ship/{target_name}/BepInEx/config/Obeliskial_importing'))


#Functions
def copy_folders(from_path,to_path):
    if os.path.exists(to_path):
        shutil.rmtree(to_path)
    shutil.copytree(from_path, to_path)

def copy_file(from_path,to_path,file):
    if os.path.isfile(to_path+"/"+file):
        os.remove(to_path+"/"+file)
    shutil.copyfile(from_path, to_path+"/"+file)

# copy configs
if enabled:
    copy_folders(config_dir, config_dest_dir)
#print(os.listdir(config_dest_dir))

# copy dll
plugin_name = f"com.binbin.{csproj_name}.dll"
trait_dir = script_dir + f"/{char_name}Traits/bin/Debug/netstandard2.1/{plugin_name}"
trait_dest_dir = os.path.abspath(os.path.join(script_dir, '..', f'Ready to Ship/{target_name}/BepInEx/plugins'))

if enabled:
    copy_file(trait_dir,trait_dest_dir,plugin_name)

# update version number
if enabled:
    with open(os.path.abspath(os.path.join(script_dir, '..', f'Ready to Ship/{target_name}/manifest.json')),"r+") as json_data:
        manifest = json.load(json_data)
        version = manifest["version_number"].split(".")
        version[-1]=str(int(version[-1])+1)
        manifest["version_number"] = ".".join(version)
        json_data.seek(0)       
        json.dump(manifest, json_data, indent=4)
        json_data.truncate()     

if load_to_local_ato:
    ato_importing = f"/Users/kevinmccoy/Library/Application Support/Steam/steamapps/common/Across the Obelisk/BepInEx/config/Obeliskial_importing/{char_name}"
    ato_plugins = "/Users/kevinmccoy/Library/Application Support/Steam/steamapps/common/Across the Obelisk/BepInEx/plugins"
    copy_folders(config_dir,ato_importing)
    copy_file(trait_dir,ato_plugins,plugin_name)
    print("Added to local!")

if enabled:
    print("Hopefully Worked!")
