import subprocess

def get_default_route(interface):
    # 获取默认路由信息
    cmd = "ip route show default"
    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)
    output = result.stdout.strip()
    lines = output.split("\n")
    for line in lines:
        if line.startswith("default") and f"dev {interface}" in line:
            return True
    return False

def set_default_route(interface):
    # 将出站流量通过指定接口设置为流量出口
    if get_default_route(interface):
        print(f"已经设置过 {interface} 的默认路由")
    else:
        gateway = get_ppp0_gateway()
        cmd = f"ip route replace default via {gateway} dev {interface}"
        subprocess.run(cmd, shell=True)
        print(f"已将 {interface} 设置为流量出口")

# 设置 ppp0 接口为流量出口（仅当不存在相同的路由时）
if not get_default_route("ppp0"):
    set_default_route("ppp0")